using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Machina.FFXIV;
using Machina.FFXIV.Oodle;
using Machina.Infrastructure;

[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

[DllImport("user32.dll", SetLastError = true)]
static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

static void WriteRecord(TCPConnection connection, long epoch, byte[] data)
{
    // Print record to stdout
    var record = new SniffRecord
    {
        Timestamp = DateTimeOffset.UtcNow,
        Version = 1,
        SourceAddress = new IPAddress(connection.LocalIP),
        SourcePort = connection.LocalPort,
        DestinationAddress = new IPAddress(connection.RemoteIP),
        DestinationPort = connection.RemotePort,
        Data = data.Select(b => (int)b).ToArray(),
    };

    Console.WriteLine(JsonSerializer.Serialize(record));
}

// Fetch active game instance
var window = FindWindow("FFXIVGAME", null);

// Returns the ID of the thread that created the window, which we don't care about
_ = GetWindowThreadProcessId(window, out var pid);
if (pid == 0)
{
    // We do need the process ID
    Console.Error.WriteLine("Failed to detect game instance");
    return 1;
}

Process proc;
try
{
    proc = Process.GetProcessById(Convert.ToInt32(pid));
}
catch (Exception e)
{
    Console.Error.WriteLine($"Failed to retrieve game process by process ID (pid={pid})");
    Console.Error.WriteLine(e.ToString());
    return 1;
}

string gamePath;
try
{
    var fileName = proc.MainModule?.FileName;
    if (string.IsNullOrEmpty(fileName))
    {
        Console.Error.WriteLine("Failed to retrieve game path from instance");
        return 1;
    }

    gamePath = fileName;
}
catch (Exception e)
{
    Console.Error.WriteLine("Failed to access game process main module");
    Console.Error.WriteLine(e.ToString());
    return 1;
}

// Start network monitor
var monitor = new FFXIVNetworkMonitor
{
    MonitorType = NetworkMonitorType.WinPCap,
    ProcessID = pid,
    MessageReceivedEventHandler = WriteRecord,
    MessageSentEventHandler = WriteRecord,
    OodleImplementation = OodleImplementation.Ffxiv,
    OodlePath = gamePath,
};

monitor.Start();
await Task.Delay(-1);

return 0;

internal class SniffRecord
{
    [JsonPropertyName("t")]
    [JsonPropertyOrder(0)]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("v")]
    [JsonPropertyOrder(1)]
    public int Version { get; set; }

    [JsonPropertyName("src_addr")]
    [JsonPropertyOrder(2)]
    [JsonConverter(typeof(IPAddressConverter))]
    public IPAddress? SourceAddress { get; set; }

    [JsonPropertyName("src_port")]
    [JsonPropertyOrder(3)]
    public int SourcePort { get; set; }

    [JsonPropertyName("dst_addr")]
    [JsonPropertyOrder(4)]
    [JsonConverter(typeof(IPAddressConverter))]
    public IPAddress? DestinationAddress { get; set; }

    [JsonPropertyName("dst_port")]
    [JsonPropertyOrder(5)]
    public int DestinationPort { get; set; }

    [JsonPropertyName("data")]
    [JsonPropertyOrder(6)]
    public int[]? Data { get; set; }
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