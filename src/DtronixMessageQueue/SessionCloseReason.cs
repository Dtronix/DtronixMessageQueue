namespace DtronixMessageQueue
{
    /// <summary>
    /// CloseReason enum
    /// </summary>
    public enum SessionCloseReason : byte
    {
        /// <summary>
        /// The socket is closed for unknown reason
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Close for server shutdown
        /// </summary>
        //ServerShutdown = 1,

        /// <summary>
        /// The client close the socket
        /// </summary>
        Closing = 2,

        /// <summary>
        /// The server side close the socket
        /// </summary>
        //ServerClosing = 3,

        /// <summary>
        /// Application error
        /// </summary>
        ApplicationError = 4,

        /// <summary>
        /// The socket is closed for a socket error
        /// </summary>
        SocketError = 5,

        /// <summary>
        /// The socket is closed by server for timeout
        /// </summary>
        TimeOut = 6,

        /// <summary>
        /// Protocol error 
        /// </summary>
        ProtocolError = 7,

        /// <summary>
        /// MessageQueue internal error
        /// </summary>
        InternalError = 8,

        /// <summary>
        /// Occurs when the session does not supply the proper connection information.
        /// </summary>
        AuthenticationFailure = 9,

        /// <summary>
        /// Occurs when the session does not pass an authorization check.
        /// </summary>
        AuthorizationFailure = 10,

        /// <summary>
        /// Occurs when the server has reached the maximum number of clients that it is allowing to connect.
        /// </summary>
        ConnectionRefused = 11
    }
}