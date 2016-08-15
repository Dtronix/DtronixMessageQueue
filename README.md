DtronixMessageQueue [![Build Status](https://travis-ci.org/Dtronix/DtronixMessageQueue.svg?branch=master)](https://travis-ci.org/Dtronix/DtronixMessageQueue)
============
DtronixMessageQueue is a small .net TCP/UDP message queueing system based upon [kerryjiang's SuperSocket](https://github.com/kerryjiang/SuperSocket)

The purpose of this project is to provide a simple transport protocol for multiple systems, mostly being the DtronixRpc system.

### Performance
[Laptop Intel i7-6500U 16GB](DtronixMessageQueue.Tests.Performance/Results/i7-6500U-16GB.md)

[Desktop Intel i5-3470 8GB](DtronixMessageQueue.Tests.Performance/Results/i5-3470-8GB.md)

Messages sizes exclude the 3 bytes header for the packet and the 3 byte header for each frame.
Each message is 4 frames long.

### License
Released under MIT license
