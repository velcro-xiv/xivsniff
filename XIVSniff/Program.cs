using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Machina.FFXIV;
using Machina.FFXIV.Oodle;
using Machina.Infrastructure;

// Fetch active game instance
var window = FindWindow("FFXIVGAME", null);

// Returns the ID of the thread that created the window, which we don't care about
_ = GetWindowThreadProcessId(window, out var pid);
if (pid == 0)
{
    // We do need the process ID
    PrintError("Failed to detect game instance");
    return 1;
}

Process proc;
try
{
    proc = Process.GetProcessById(Convert.ToInt32(pid));
}
catch (Exception e)
{
    PrintError($"Failed to retrieve game process by process ID (pid={pid})");
    PrintError(e.ToString());
    return 1;
}

string gamePath;
try
{
    gamePath = GetGamePath(proc);
}
catch (Exception e)
{
    PrintError("Failed to access game process main module");
    PrintError(e.ToString());
    return 1;
}

// Start network monitor
var monitor = new FFXIVNetworkMonitor
{
    MonitorType = NetworkMonitorType.WinPCap,
    ProcessID = pid,
    MessageReceivedEventHandler = MessageReceived,
    MessageSentEventHandler = MessageSent,
    OodleImplementation = OodleImplementation.Ffxiv,
    OodlePath = gamePath,
};

monitor.Start();
await Task.Delay(-1);

return 0;

[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

[DllImport("user32.dll", SetLastError = true)]
static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

static void PrintError(string err)
{
    Console.Error.WriteLine(err);
}

static string GetGamePath(Process proc)
{
    var fileName = proc.MainModule?.FileName;
    if (string.IsNullOrEmpty(fileName))
    {
        throw new InvalidOperationException("Failed to retrieve game path from instance");
    }

    return fileName;
}

static SniffRecord CreateRecord(IPAddress sourceAddr, ushort sourcePort, IPAddress destAddr, ushort destPort,
    byte[] data)
{
    var segmentType = BitConverter.ToUInt16(data, 0xC);
    return new SniffRecord
    {
        Timestamp = DateTimeOffset.UtcNow,
        Version = 2,
        SourceAddress = sourceAddr,
        SourcePort = sourcePort,
        DestinationAddress = destAddr,
        DestinationPort = destPort,
        SegmentHeader = SegmentHeader.Read(data, 0),
        MessageHeader = segmentType == 3 ? MessageHeader.Read(data, 0x10) : null,
        MessageData = segmentType == 3 ? data.Skip(0x20).Select(b => (int)b).ToArray() : null,
    };
}

static void PrintRecord(SniffRecord record)
{
    try
    {
        Console.WriteLine(JsonSerializer.Serialize(record));
    }
    catch (Exception e)
    {
        PrintError("Failed to print record");
        PrintError(e.ToString());
    }
}

static void MessageSent(TCPConnection connection, long epoch, byte[] data)
{
    // Print record to stdout
    var record = CreateRecord(new IPAddress(connection.LocalIP), connection.LocalPort,
        new IPAddress(connection.RemoteIP),
        connection.RemotePort, data);
    PrintRecord(record);
}

static void MessageReceived(TCPConnection connection, long epoch, byte[] data)
{
    // Print record to stdout
    var record = CreateRecord(new IPAddress(connection.RemoteIP), connection.RemotePort,
        new IPAddress(connection.LocalIP),
        connection.LocalPort, data);
    PrintRecord(record);
}

internal class MessageHeader
{
    [JsonPropertyName("opcode")] public ushort Opcode { get; set; }

    [JsonPropertyName("server")] public ushort Server { get; set; }

    [JsonPropertyName("t")] public uint Timestamp { get; set; }

    public static MessageHeader Read(byte[] data, int offset)
    {
        return new MessageHeader
        {
            Opcode = BitConverter.ToUInt16(data, offset + 2),
            Server = BitConverter.ToUInt16(data, offset + 6),
            Timestamp = BitConverter.ToUInt32(data, offset + 8),
        };
    }
}

internal class SegmentHeader
{
    [JsonPropertyName("size")] public uint Size { get; set; }

    [JsonPropertyName("source_actor")] public uint SourceActor { get; set; }

    [JsonPropertyName("target_actor")] public uint TargetActor { get; set; }

    [JsonPropertyName("type")] public ushort Type { get; set; }

    public static SegmentHeader Read(byte[] data, int offset)
    {
        return new SegmentHeader
        {
            Size = BitConverter.ToUInt32(data, offset),
            SourceActor = BitConverter.ToUInt32(data, offset + 4),
            TargetActor = BitConverter.ToUInt32(data, offset + 8),
            Type = BitConverter.ToUInt16(data, offset + 12),
        };
    }
}

internal class SniffRecord
{
    [JsonPropertyName("t")] public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("v")] public int Version { get; set; }

    [JsonPropertyName("src_addr")]
    [JsonConverter(typeof(IPAddressConverter))]
    public IPAddress? SourceAddress { get; set; }

    [JsonPropertyName("src_port")] public int SourcePort { get; set; }

    [JsonPropertyName("dst_addr")]
    [JsonConverter(typeof(IPAddressConverter))]
    public IPAddress? DestinationAddress { get; set; }

    [JsonPropertyName("dst_port")] public int DestinationPort { get; set; }

    [JsonPropertyName("segment_header")] public SegmentHeader? SegmentHeader { get; set; }

    [JsonPropertyName("message_header")] public MessageHeader? MessageHeader { get; set; }

    [JsonPropertyName("message_data")] public int[]? MessageData { get; set; }
}

internal class IPAddressConverter : JsonConverter<IPAddress>
{
    public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return IPAddress.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}