DtronixMessageQueue [![Build Status](https://travis-ci.org/Dtronix/DtronixMessageQueue.svg?branch=master)](https://travis-ci.org/Dtronix/DtronixMessageQueue)
============
DtronixMessageQueue is a small .net TCP/UDP message queueing system using the microsoft [SocketAsyncEventArgs](https://msdn.microsoft.com/en-us/library/system.net.sockets.socketasynceventargs(v=vs.110).aspx) interface

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance

[Laptop Intel i7-6500U 16GB](DtronixMessageQueue.Tests.Performance/Results/i7-6500U-16GB.md)

[Desktop Intel i5-3470 8GB 8KB Buffer](DtronixMessageQueue.Tests.Performance/Results/i5-3470-8GB-8KB.md)

[Desktop Intel i5-3470 8GB 16KB Buffer](DtronixMessageQueue.Tests.Performance/Results/i5-3470-8GB-16KB.md)

[Desktop Intel i7-6700K 32GB 8KB Buffer](DtronixMessageQueue.Tests.Performance/Results/i7-6700K-32GB.md)

### License
Released under [MIT license](LICENSE)
