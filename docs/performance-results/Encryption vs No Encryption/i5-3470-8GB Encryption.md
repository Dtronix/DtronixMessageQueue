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
| Release |  1,000,000 |        40 |        1,145 |    873,362 |    34.93 |
| Release |  1,000,000 |        40 |        1,126 |    888,099 |    35.52 |
| Release |  1,000,000 |        40 |        1,130 |    884,955 |    35.40 |
| Release |  1,000,000 |        40 |        1,134 |    881,834 |    35.27 |
| Release |  1,000,000 |        40 |        1,168 |    856,164 |    34.25 |
|         |            |  AVERAGES |        1,141 |    876,883 |    35.08 |

|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |    100,000 |     2,000 |        1,166 |     85,763 |   171.53 |
| Release |    100,000 |     2,000 |        1,209 |     82,712 |   165.43 |
| Release |    100,000 |     2,000 |        1,790 |     55,865 |   111.73 |
| Release |    100,000 |     2,000 |        1,231 |     81,234 |   162.47 |
| Release |    100,000 |     2,000 |        1,203 |     83,125 |   166.25 |
|         |            |  AVERAGES |        1,320 |     77,740 |   155.48 |

|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |     10,000 |    60,048 |        3,149 |      3,175 |   190.69 |
| Release |     10,000 |    60,048 |        3,123 |      3,202 |   192.28 |
| Release |     10,000 |    60,048 |        3,119 |      3,206 |   192.52 |
| Release |     10,000 |    60,048 |        3,112 |      3,213 |   192.96 |
| Release |     10,000 |    60,048 |        3,145 |      3,179 |   190.93 |
|         |            |  AVERAGES |        3,130 |      3,195 |   191.88 |

RPC Performance tests.

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |  NoReturn |    200,000 |        1,753 |    114,090 |
| Release |  NoReturn |    200,000 |        1,733 |    115,406 |
| Release |  NoReturn |    200,000 |        1,711 |    116,890 |
| Release |  NoReturn |    200,000 |        1,734 |    115,340 |
|         |           |   AVERAGES |        1,733 |    115,432 |

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |     Await |    200,000 |        2,525 |     79,207 |
| Release |     Await |    200,000 |        2,744 |     72,886 |
| Release |     Await |    200,000 |        2,731 |     73,233 |
| Release |     Await |    200,000 |        2,748 |     72,780 |
|         |           |   AVERAGES |        2,687 |     74,527 |

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |    Return |     10,000 |        1,394 |      7,173 |
| Release |    Return |     10,000 |        1,359 |      7,358 |
| Release |    Return |     10,000 |        1,347 |      7,423 |
| Release |    Return |     10,000 |        1,348 |      7,418 |
|         |           |   AVERAGES |        1,362 |      7,343 |

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release | Exception |      2,000 |          512 |      3,906 |
| Release | Exception |      2,000 |          484 |      4,132 |
| Release | Exception |      2,000 |          483 |      4,140 |
| Release | Exception |      2,000 |          482 |      4,149 |
|         |           |   AVERAGES |          490 |      4,082 |

