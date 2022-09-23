# xivsniff
A simple packet sniffer for FFXIV.
Produces packets as [JSON Lines](https://jsonlines.org/) data to be piped into other programs.
Data can also be piped to a file for future processing. `xivsniff` is designed to be used with
[velcro](https://github.com/velcro-xiv/velcro).

Requires WinPcap or an equivalent driver such as [npcap](https://npcap.com/).

## Usage

### Standalone
```zsh
xivsniff
```

### With `velcro`
```zsh
xivsniff | velcro
```

## Format
Line records consist of:
* [ISO 8601](https://en.wikipedia.org/wiki/ISO_8601) timestamp, normalized to UTC
* Version number
* [Segment type](https://github.com/SapphireServer/Sapphire/blob/991e0551c32728f1f4b91a88b940881f29228a48/src/common/Network/CommonNetwork.h#L134-L146)
* Opcode (nullable, only non-null in IPC segments)
* Source IP address
* Source port
* Destination IP address
* Destination port
* Packet data as a number array

### Example
```json lines
{"t": "2022-09-22T00:51:57.721Z", "v": 1, "src_addr": "192.168.1.155", "src_port": 55321, "dst_addr": "203.0.113.18", "dst_port": 54651, "data": [115, 111, 109, 101, 66, 79, 68, 89, 32, 111, 110, 99, 101, 32, 116, 111, 108, 100, 32, 109, 101, 32, 116, 104, 101]}
{"t": "2022-09-22T01:02:27.648Z", "v": 1, "src_addr": "192.168.1.155", "src_port": 55321, "dst_addr": "203.0.113.18", "dst_port": 54651, "data": [119, 111, 114, 108, 100, 32, 119, 97, 115, 32, 103, 111, 110, 110, 97, 32, 114, 111, 108, 108, 32, 109, 101]}
```
