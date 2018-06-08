## Overview
DtronixMessageQueue utilizes a custom transport protocol which utilizes several of the technologies used in the TLS protocol.  A full blown TLS protocol is a little overkill since authentication and encryption negotiation is not required. Forward-security is a corner stone of this transport protocol.  To facilitate that functionality the Diffie–Hellman key exchange methods are used with ephemeral keys generated for each connection.


## Protocol
### Records
Records are used to define the state of the protocol, send alerts and transport data


https://www.tablesgenerator.com/markdown_tables#

|   Client   | Direction |     Server     | Notes                                            |
|:----------:|:---------:|:--------------:|--------------------------------------------------|
|   CONNECT  |     ->    |        -       | Initial connection from server                   |
|      -     |     <-    |     SHello     | Contains protocol version & setup information    |
|      -     |     <-    |   SChangeKey   | Contains the first half of the D-H key exchange  |
| CChangeKey |     ->    |        -       | Contains the second half of the D-H key exchange |
|      -     |     <-    | SChangeKeyDone | Completes the encryption                         |
|            |           |                |                                                  |
|            |           |                |                                                  |
|            |           |                |                                                  |
