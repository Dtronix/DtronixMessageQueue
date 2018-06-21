Intel(R) Core(TM) i5-3470 CPU @ 3.20GHz with 8 GB of RAM installed.
DMQPerf.exe 
MQ Performance tests.

FrameBufferSize: 16381; SendAndReceiveBufferSize: 16384

|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |          1 |        40 |            5 |        200 |     0.01 |
|         |            |  AVERAGES |            5 |        200 |     0.01 |

|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |  1,000,000 |        40 |        1,044 |    957,854 |    38.31 |
| Release |  1,000,000 |        40 |        1,017 |    983,284 |    39.33 |
| Release |  1,000,000 |        40 |        1,019 |    981,354 |    39.25 |
| Release |  1,000,000 |        40 |        1,021 |    979,431 |    39.18 |
| Release |  1,000,000 |        40 |        1,023 |    977,517 |    39.10 |
|         |            |  AVERAGES |        1,025 |    975,888 |    39.04 |

|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |    100,000 |     2,000 |          650 |    153,846 |   307.69 |
| Release |    100,000 |     2,000 |          651 |    153,609 |   307.22 |
| Release |    100,000 |     2,000 |          665 |    150,375 |   300.75 |
| Release |    100,000 |     2,000 |          665 |    150,375 |   300.75 |
| Release |    100,000 |     2,000 |          665 |    150,375 |   300.75 |
|         |            |  AVERAGES |          659 |    151,716 |   303.43 |

|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |     10,000 |    60,048 |        1,607 |      6,222 |   373.67 |
| Release |     10,000 |    60,048 |        1,615 |      6,191 |   371.81 |
| Release |     10,000 |    60,048 |        1,622 |      6,165 |   370.21 |
| Release |     10,000 |    60,048 |        1,653 |      6,049 |   363.27 |
| Release |     10,000 |    60,048 |        1,617 |      6,184 |   371.35 |
|         |            |  AVERAGES |        1,623 |      6,162 |   370.06 |

RPC Performance tests.

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |  NoReturn |    200,000 |        1,699 |    117,716 |
| Release |  NoReturn |    200,000 |        1,690 |    118,343 |
| Release |  NoReturn |    200,000 |        1,682 |    118,906 |
| Release |  NoReturn |    200,000 |        1,701 |    117,577 |
|         |           |   AVERAGES |        1,693 |    118,136 |

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |     Await |    200,000 |        2,567 |     77,911 |
| Release |     Await |    200,000 |        2,687 |     74,432 |
| Release |     Await |    200,000 |        2,733 |     73,179 |
| Release |     Await |    200,000 |        2,705 |     73,937 |
|         |           |   AVERAGES |        2,673 |     74,865 |

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |    Return |     10,000 |        1,416 |      7,062 |
| Release |    Return |     10,000 |        1,368 |      7,309 |
| Release |    Return |     10,000 |        1,366 |      7,320 |
| Release |    Return |     10,000 |        1,367 |      7,315 |
|         |           |   AVERAGES |        1,379 |      7,252 |

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release | Exception |      2,000 |          503 |      3,976 |
| Release | Exception |      2,000 |          458 |      4,366 |
| Release | Exception |      2,000 |          457 |      4,376 |
| Release | Exception |      2,000 |          456 |      4,385 |
|         |           |   AVERAGES |          469 |      4,276 |

