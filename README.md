DtronixMessageQueue [![Build Status](https://travis-ci.org/Dtronix/DtronixMessageQueue.svg?branch=master)](https://travis-ci.org/Dtronix/DtronixMessageQueue) [![NuGet](https://img.shields.io/nuget/v/DtronixMessageQueue.svg?maxAge=600)](https://www.nuget.org/packages/DtronixMessageQueue)
============
DtronixMessageQueue is a small .net TCP/UDP message queueing system using the microsoft [SocketAsyncEventArgs](https://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs(v=vs.110).aspx) interface

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance

#### DtronixMessageQueue

Sample performance tests.  Numbers are averages from 5 loops of the performance test program. [Full Performance Test](docs/performance-results/i5-3470-8GB-16KB.md)

|   Build |   Messages | Msg Bytes | Milliseconds |        MPS |     MBps |
|---------|------------|-----------|--------------|------------|----------|
| Release |  1,000,000 |       200 |        1,253 |    798,200 |   159.64 |
| Release |    100,000 |     2,000 |          638 |    156,744 |   313.49 |
| Release |     10,000 |    60,048 |        1,868 |      5,352 |   321.40 |

[Laptop Intel i7-6500U 16GB](docs/performance-results/i7-6500U-16GB-16KB.md)

[Desktop Intel i5-3470 8GB 8KB Buffer](docs/performance-results/i5-3470-8GB-8KB.md)

[Desktop Intel i5-3470 8GB 16KB Buffer](docs/performance-results/i5-3470-8GB-16KB.md)

[Desktop Intel i7-6700K 32GB 8KB Buffer](docs/performance-results/i7-6700K-32GB-16KB.md)

#### DtronixMessageQueue.Rpc

Sample performance tests for RPC calls.  Numbers are averages from 4 loops of the Rpc performance test program. [Full Performance Test](docs/performance-results/rpc/i5-3470-8GB-rpc.md)

|   Build | Type      |   Calls    | Milliseconds |    RPC/sec |
|---------|-----------|------------|--------------|------------|
| Release |  NoRetrun |    200,000 |        1,537 |    130,134 |
| Release |    Return |     10,000 |        1,128 |      8,869 |
| Release | Exception |     10,000 |        2,484 |      4,026 |


### License
Released under [MIT license](LICENSE)
