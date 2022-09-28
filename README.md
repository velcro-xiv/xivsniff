# xivsniff
A simple packet sniffer for FFXIV.
Produces packets as [JSON Lines](https://jsonlines.org/) data to be piped into other programs.
Data can also be piped to a file for future processing. `xivsniff` is designed to be used with
[`velcro`](https://github.com/velcro-xiv/velcro).

## Requirements
* WinPcap or an equivalent driver such as [npcap](https://npcap.com/)

## Usage

### Standalone
```zsh
xivsniff
```

### With `velcro`
```zsh
xivsniff | velcro
```

#### Powershell
Powershell has its own conventions distinct from `cmd` and `bash`-based shells. Because of this, pipes into typical programs require special handling. It's best to just avoid Powershell when using `velcro`. However, you can force it to work with something like this:
```pwsh
xivsniff | Out-String -stream | velcro
```

## Format
Line records consist of:
* [ISO 8601](https://en.wikipedia.org/wiki/ISO_8601) timestamp, normalized to UTC
* Version number
* Source IP address
* Source port
* Destination IP address
* Destination port
* [Segment header](https://github.com/SapphireServer/Sapphire/blob/develop/src/common/Network/CommonNetwork.h#L80-L106)
* [Message header](https://github.com/SapphireServer/Sapphire/blob/develop/src/common/Network/CommonNetwork.h#L148-L169) (If present)
  * This contains an additional UNIX timestamp that is left in its raw format for accuracy.
* Message data as a number array (If present)

### Example
```json lines
{"t": "2022-09-22T00:51:57.721Z", "v": 2, "src_addr": "192.168.1.155", "src_port": 55321, "dst_addr": "203.0.113.18", "dst_port": 54651, "segment_header": {"size": 64, "source_actor": 1002945421, "target_actor": 1002945421, "type": 3}, "message_header": {"opcode": 645, "server": 2312, "timestamp": 1663807917}, "message_data": [115, 111, 109, 101, 66, 79, 68, 89, 32, 111, 110, 99, 101, 32, 116, 111, 108, 100, 32, 109, 101, 32, 116, 104, 101, 0, 0, 0, 0, 0, 0, 0]}
{"t": "2022-09-22T01:02:27.648Z", "v": 2, "src_addr": "192.168.1.155", "src_port": 55321, "dst_addr": "203.0.113.18", "dst_port": 54651, "segment_header": {"size": 64, "source_actor": 1002945421, "target_actor": 1002945421, "type": 3}, "message_header": {"opcode": 645, "server": 2312, "timestamp": 1663808547}, "message_data": [119, 111, 114, 108, 100, 32, 119, 97, 115, 32, 103, 111, 110, 110, 97, 32, 114, 111, 108, 108, 32, 109, 101, 0, 0, 0, 0, 0, 0, 0, 0, 0]}
```
