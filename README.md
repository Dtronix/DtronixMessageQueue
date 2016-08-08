DtronixMessageQueue
============
DtronixMessageQueue is a small .net TCP/UDP message queueing system based upon [kerryjiang's SuperSocket](https://github.com/kerryjiang/SuperSocket)

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance
Running the MqPerformanceTests.exe on my 3.2GHz Intel i5-3470 with 8 gb ram performed as follows

| Build Type   | Messages     | Milliseconds | MPS        | MBps     |
|--------------|--------------|--------------|------------|----------|
|      Release |    1,000,000 |        1,505 |    664,451 |   132.89 |
|      Release |    1,000,000 |        1,338 |    747,384 |   149.48 |
|      Release |    1,000,000 |        1,402 |    713,266 |   142.65 |
|        Debug |    1,000,000 |        3,244 |    308,261 |    61.65 |
|        Debug |    1,000,000 |        3,448 |    290,023 |    58.00 |
|        Debug |    1,000,000 |        3,345 |    298,953 |    59.79 |

Messages were 200 bytes long (excluding the 3 bytes header for the packet and excluding the 3 byte header for the frame) and contained 4 frames each.

### License
Released under MIT license
