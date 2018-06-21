## Overview
DtronixMessageQueue utilizes a custom transport protocol which utilizes several of the technologies used in the TLS protocol.  A full blown TLS protocol is a little overkill since authentication and encryption type negotiation is not required. Forward-security is a corner stone of this transport protocol.  To facilitate that functionality the Diffie–Hellman key exchange methods are used with ephemeral keys generated for each connection.

Once the key echance occurs, the chanels are encrypted with AES-256.

All transmissions are sent in blocks of 16 bytes.  This aligns with the 16 byte blocks utilized by 128 bit AES encryption.

## Protocol
### Pre-Encryption Negotiation Setup


|     Client     | Direction |     Server     | Notes                                            |
|:--------------:|:---------:|:--------------:|--------------------------------------------------|
|     CONNECT    |     ->    |        -       | Initial connection from server                   |
| EncryptChannel |     ->    |        -       | Contains the first half of the D-H key exchange  |
|        -       |     <-    | EncryptChannel | Contains the second half of the D-H key exchange |

### Types of Headers

| Name | Frame bytes | MqFrameType<br/>(byte) | Body Length<br/>(ushort?) | Payload <br/>(byte[]?) | Description |
|-----------------|:-----------:|:----------------------:|:-------------------------:|:----------------------:|-----------------------------------------------------------|
| Unset | 1 | 0 | - | - | Initial state for all headers. |
| BodyPayload | 3 | 1 | ushort [2 bytes] | byte[] | Contains a body. |
| Padding | 1 | 2 | - | - | Single byte header.  Used to pad to reach 16 byte blocks. |
| ConnectionClose | 2 | 3 | - | - | Contains a byte stating the reason the seesion closed. |
| EncryptChannel | 1 | 4 | byte[140] | - | DH Key exchange process. |

#### Header Type BodyPayload
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
    <td colspan="8">1 (byte) [8]</td><td colspan="16">Body Length (uint16) [16]</td>
  </tr>
  <tr align="center">
    <td colspan="24">Body Data (byte[]) [...]</td>
  </tr>
</table>

#### Header Type Padding
<table>
  <tr align="center">
    <td colspan="8">0</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">2 (byte) [8]</td>
  </tr>
</table>

#### Header Type ConnectionClose
<table>
  <tr align="center">
    <td colspan="8">0</td> <td colspan="8">1</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">3 (byte) [8]</td><td colspan="16">CloseReason(byte) [8]</td>
  </tr>
</table>

#### Header Type EncryptChanel
<table>
  <tr align="center">
    <td colspan="8">0</td>
  </tr>
  <tr>
    <td>0</td><td>1</td><td>2</td><td>3</td><td>4</td><td>5</td><td>6</td><td>7</td>
  </tr>
  <tr align="center">
    <td colspan="8">4 (byte) [8]</td>
  </tr>
    <tr align="center">
    <td colspan="8">DH public key (byte) [140]</td>
  </tr>
</table>
