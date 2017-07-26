DtronixMessageQueue [![Build Status](https://travis-ci.org/Dtronix/DtronixMessageQueue.svg?branch=master)](https://travis-ci.org/Dtronix/DtronixMessageQueue) [![NuGet](https://img.shields.io/nuget/v/DtronixMessageQueue.svg?maxAge=600)](https://www.nuget.org/packages/DtronixMessageQueue)
============
DtronixMessageQueue is a small .net TCP/UDP message queueing system using the microsoft [SocketAsyncEventArgs](https://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs(v=vs.110).aspx) interface

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance

#### DtronixMessageQueue

Sample performance tests.  Numbers are averages from 5 loops of the performance test program. [Full Performance Test](docs/performance-results/i7-6800K-16GB.md)

Intel(R) Core(TM) i7-6800K CPU @ 3.40GHz with 16 GB of RAM installed.

|   Build |   Messages | Msg Bytes | Milliseconds |    Msg/sec |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |  1,000,000 |       200 |        1,481 |    675,734 |   135.15 |
| Release |    100,000 |     2,000 |          502 |    199,224 |   398.45 |
| Release |     10,000 |    60,048 |        1,251 |      7,993 |   480.01 |

#### DtronixMessageQueue.Rpc

Sample performance tests for RPC calls.  Numbers are averages from 4 loops of the Rpc performance test program. [Full Performance Test](docs/performance-results/i7-6800K-16GB.md)

Intel(R) Core(TM) i7-6800K CPU @ 3.40GHz with 16 GB of RAM installed.

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |  NoReturn |    200,000 |        1,715 |    116,673 |
| Release |     Await |    200,000 |        3,179 |     62,927 |
| Release |     Block |        100 |          999 |      3,187 |
| Release |    Return |     10,000 |        1,062 |      9,420 |
| Release | Exception |     10,000 |        3,703 |      2,702 |



### License
Released under [MIT license](LICENSE)
