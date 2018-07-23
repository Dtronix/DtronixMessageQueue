DtronixMessageQueue [![Build Status](https://travis-ci.org/Dtronix/DtronixMessageQueue.svg?branch=master)](https://travis-ci.org/Dtronix/DtronixMessageQueue) [![NuGet](https://img.shields.io/nuget/v/DtronixMessageQueue.svg?maxAge=600)](https://www.nuget.org/packages/DtronixMessageQueue)
============
DtronixMessageQueue is a small .net TCP message queueing system using the microsoft [SocketAsyncEventArgs](https://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs(v=vs.110).aspx) interface.

All transported information is encrypted by 256 bit AES.

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance

#### DtronixMessageQueue

Sample performance tests.  Numbers are averages from 5 loops of the performance test program. [Full Performance Test](docs/performance-results/i5-3470-8GB.md)

Intel(R) Core(TM) i5-3470 CPU @ 3.20GHz with 8 GB of RAM installed.

|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |          1 |        40 |            5 |        200 |     0.01 |
| Release |  1,000,000 |        40 |        1,193 |    838,580 |    33.54 |
| Release |    100,000 |     2,000 |        1,222 |     81,851 |   163.70 |
| Release |     10,000 |    60,048 |        2,953 |      3,385 |   203.32 |

#### DtronixMessageQueue.Rpc

Sample performance tests for RPC calls.  Numbers are averages from 4 loops of the Rpc performance test program. [Full Performance Test](docs/performance-results/i5-3470-8GB.md)

Intel(R) Core(TM) i5-3470 CPU @ 3.20GHz with 8 GB of RAM installed.

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |  NoReturn |    200,000 |        1,859 |    107,632 |
| Release |     Await |    200,000 |        2,794 |     71,614 |
| Release |    Return |     10,000 |        1,595 |      6,273 |
| Release | Exception |      2,000 |          552 |      3,631 |

### Protocols
DtronixMessageQueue utilizies several layers a [custom encryption protocol](docs/secured-transport.md) on top of the TCP protocol.

DtronixMessageQueie laytes the [message queue protocol](docs/dtronix-message-queue.md) on top of the TcpScocket protocol.

### License
Released under [MIT license](LICENSE)
