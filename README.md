DtronixMessageQueue
============
DtronixMessageQueue is a small .net TCP/UDP message queueing system based upon [kerryjiang's SuperSocket](https://github.com/kerryjiang/SuperSocket)

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance
Running the MqPerformanceTests.exe on my 2.5 Ghz laptop with 16 gb ram performed as follows

| Build Type   | Messages    | Miliseconds      |  MPS       |
| -------------|-------------|------------------|----------- |
| Release      | 1,000,000   | 2,721            |  367,511   |
| Debug        | 1,000,000   | 4,326            |  231,160   |

Messages were 215 bytes long excluding the 3 bytes header for the packet and contained 4 frames each.

### License
Released under MIT license
