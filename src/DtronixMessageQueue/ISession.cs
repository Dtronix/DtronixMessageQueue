using System;
using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue
{
    public interface ISession
    {

        SessionMode Mode { get; }

        /// <summary>
        /// Invoked when session has received data from the peer.
        /// </summary>
        Action<ReadOnlyMemory<byte>> Received { get; set; }

        /// <summary>
        /// Invoked when session completed sending data to the peer.
        /// </summary>
        Action<ISession> Sent { get; set; }

        /// <summary>
        /// Fired when session is disconnected from the peer.
        /// </summary>
        event EventHandler<SessionEventArgs> Disconnected;

        /// <summary>
        /// Fired when the session has completed the connection to the peer.
        /// </summary>
        event EventHandler<SessionEventArgs> Connected;

        SessionState State { get; }

        void Disconnect();

        void Send(ReadOnlyMemory<byte> buffer);
    }
}
