## Overview
DtronixMessageQueue is a small, fast and unobtrusive Client/Server which is based upon the TCP/IP protocol to aid in simplifying transmitting of data across the wire.

#### Why?
I started off with learning ZeroMQ through and through.  As great a framework as it is, it abstracts too much away from me for my needs.  Ultimately, I needed control of the base protocol to allow be to manage connections and force-ably disconnect clients that are mis-behaving or have lost connection.  With the Router-Dealer setup, this became ~~difficult~~Impossible.  At least with NetMq.

## Protocol
### Messages and Frames
The transport is a fairly simple one. One or more frames make up a message.  Messages are used to transport the frames across the wire.

### Messages
Messages are the packages of data that are sent across the wire to the other end.  Messages are broken up by the definition of frames which they contain.  A message contains at least one frame otherwise it will be discarded because a message with no frames literally has no data to send.

Messages are intended to group information together.  **Frames inside messages are always guaranteed to be received in the same order but messages are not. **  This is due to internal optimization to speed up transfer of messages across the wire

### Frames
Messages are broken up into individual frames.  In-fact, the wire protocol has no concept of messages at all, it sees only individual frames. which means that frame types need to define the end of messages. Frames are structured as follows.

### Standard MqFrame
<table>
  <tr align="center">
    <td colspan="8">0</td> <td colspan="8">1</td> <td colspan="8">2</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">Frame Type (byte) [8]</td><td colspan="16">Frame Length (uint16) [16]</td>
  </tr>
  <tr align="center">
    <td colspan="24">Frame Data (byte[]) [...]</td>
  </tr>
</table>


Each frame contains at the very minimum 1 byte.  This byte is used to determine what type of frame is being read.  Depending on the MqFrameType, the frame might be 1 or more bytes long.  See Types of Frames below for all the types of frames and their payload.


### Types of Frames
There are seven types of frames, but only six that are used to send across the wire.  The Unset type is never used except as the initial state for the frame.

| Name      | Frame bytes | MqFrameType<br/>(byte) |  Message Length<br/>(ushort?)| Payload<br/>(byte[]?) | Description       |
|-----------|:-----------:|:-----------:|:----------------:|:-------:|--------------------------------------------------------|
|   Unset   |      0      |      0      |         -        |    -    | Initial state for all frames.                          |
|   Empty   |      1      |      1      |         -        |    -    | No body                                                |
|    More   |    \>= 3    |      2      | ushort [2 bytes] |  byte[] | Contains a body.                                       |
|    Last   |    \>= 3    |      3      | ushort [2 bytes] |  byte[] | Contains a body and is the last frame in this message. |
| EmptyLast |      1      |      4      |         -        |    -    | Empty and the last frame in the message.               |
|  Command  |    \>= 3    |      5      | ushort [2 bytes] |  byte[] | Command to be processed and consumed internally.       |
|    Ping   |      1      |      6      |         -        |    -    | Same as EmptyLast frame but consumed internally.       |

##### MqFrame Type Empty
<table>
  <tr align="center">
    <td colspan="8">0</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">1 (byte) [8]</td>
</table>

##### MqFrame Type More
<table>
  <tr align="center">
    <td colspan="8">0</td> <td colspan="8">1</td> <td colspan="8">2</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">2 (byte) [8]</td><td colspan="16">Frame Length (uint16) [16]</td>
  </tr>
  <tr align="center">
    <td colspan="24">Frame Data (byte[]) [...]</td>
  </tr>
</table>

##### MqFrame Type Last
<table>
  <tr align="center">
    <td colspan="8">0</td> <td colspan="8">1</td> <td colspan="8">2</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">3 (byte) [8]</td><td colspan="16">Frame Length (uint16) [16]</td>
  </tr>
  <tr align="center">
    <td colspan="24">Frame Data (byte[]) [...]</td>
  </tr>
</table>

##### MqFrame Type EmptyLast
<table>
  <tr align="center">
    <td colspan="8">0</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">4 (byte) [8]</td>
</table>

##### MqFrame Type Command
<table>
  <tr align="center">
    <td colspan="8">0</td> <td colspan="8">1</td> <td colspan="8">2</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">5 (byte) [8]</td><td colspan="16">Frame Length (uint16) [16]</td>
  </tr>
  <tr align="center">
    <td colspan="24">Frame Data (byte[]) [...]</td>
  </tr>
</table>


##### MqFrame Type Ping
<table>
  <tr align="center">
    <td colspan="8">0</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">6 (byte) [8]</td>
</table>
