DtronixMessageQueue
============
DtronixMessageQueue is a small .net TCP/UDP message queueing system based upon [kerryjiang's SuperSocket](https://github.com/kerryjiang/SuperSocket)

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance
Running the MqPerformanceTests.exe on my 3.2GHz Intel i5-3470 with 8 gb ram performed as follows

|   Build |   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |  1,000,000 |       200 |        1,396 |    716,332 |   143.27 |
| Release |  1,000,000 |       200 |        1,419 |    704,721 |   140.94 |
| Release |  1,000,000 |       200 |        1,367 |    731,528 |   146.31 |
| Release |  1,000,000 |       200 |        1,427 |    700,770 |   140.15 |
| Release |  1,000,000 |       200 |        1,432 |    698,324 |   139.66 |
|         |            |  AVERAGES |        1,408 |    710,335 |   142.07 |

|   Build |   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |    100,000 |     2,000 |        1,045 |     95,693 |   191.39 |
| Release |    100,000 |     2,000 |        1,059 |     94,428 |   188.86 |
| Release |    100,000 |     2,000 |        1,045 |     95,693 |   191.39 |
| Release |    100,000 |     2,000 |        1,069 |     93,545 |   187.09 |
| Release |    100,000 |     2,000 |        1,074 |     93,109 |   186.22 |
|         |            |  AVERAGES |        1,058 |     94,494 |   188.99 |

|   Build |   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |     10,000 |    60,000 |        3,207 |      3,118 |   187.09 |
| Release |     10,000 |    60,000 |        3,223 |      3,102 |   186.16 |
| Release |     10,000 |    60,000 |        3,242 |      3,084 |   185.07 |
| Release |     10,000 |    60,000 |        3,227 |      3,098 |   185.93 |
| Release |     10,000 |    60,000 |        3,193 |      3,131 |   187.91 |
|         |            |  AVERAGES |        3,218 |      3,107 |   186.43 |

Messages were 200 bytes long (excluding the 3 bytes header for the packet and excluding the 3 byte header for the frame) and contained 4 frames each.

### License
Released under MIT license
