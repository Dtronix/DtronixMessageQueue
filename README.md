DtronixMessageQueue
============
DtronixMessageQueue is a small .net TCP/UDP message queueing system based upon [kerryjiang's SuperSocket](https://github.com/kerryjiang/SuperSocket)

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance
Running the MqPerformanceTests.exe on my 3.2GHz Intel i5-3470 with 8 gb ram performed as follows

|   Build |   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |  1,000,000 |       200 |        1,550 |    645,161 |   129.03 |
| Release |  1,000,000 |       200 |        1,537 |    650,618 |   130.12 |
| Release |  1,000,000 |       200 |        1,386 |    721,500 |   144.30 |
| Release |  1,000,000 |       200 |        1,401 |    713,775 |   142.76 |
| Release |  1,000,000 |       200 |        1,530 |    653,594 |   130.72 |
|---------|------------|-----------|--------------|------------|----------|
|------------------------ AVERAGES |        1,481 |    676,930 |   135.39 |
|---------|------------|-----------|--------------|------------|----------|

|   Build |   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |    100,000 |     2,000 |        1,079 |     92,678 |   185.36 |
| Release |    100,000 |     2,000 |        1,067 |     93,720 |   187.44 |
| Release |    100,000 |     2,000 |        1,070 |     93,457 |   186.92 |
| Release |    100,000 |     2,000 |        1,074 |     93,109 |   186.22 |
| Release |    100,000 |     2,000 |        1,057 |     94,607 |   189.21 |
|---------|------------|-----------|--------------|------------|----------|
|------------------------ AVERAGES |        1,069 |     93,514 |   187.03 |
|---------|------------|-----------|--------------|------------|----------|

|   Build |   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |     10,000 |    60,000 |        3,202 |      3,123 |   187.38 |
| Release |     10,000 |    60,000 |        3,208 |      3,117 |   187.03 |
| Release |     10,000 |    60,000 |        3,213 |      3,112 |   186.74 |
| Release |     10,000 |    60,000 |        3,204 |      3,121 |   187.27 |
| Release |     10,000 |    60,000 |        3,218 |      3,107 |   186.45 |
|---------|------------|-----------|--------------|------------|----------|
|------------------------ AVERAGES |        3,209 |      3,116 |   186.97 |
|---------|------------|-----------|--------------|------------|----------|

Messages were 200 bytes long (excluding the 3 bytes header for the packet and excluding the 3 byte header for the frame) and contained 4 frames each.

### License
Released under MIT license
