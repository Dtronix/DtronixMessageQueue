using System;
using System.Collections.Generic;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Event args for when a new message has been processed and is ready for usage.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class IncomingMessageEventArgs<TSession, TConfig> : EventArgs
        where TSession : MqSession<TSession, TConfig>
        where TConfig : MqConfig
    {
        /// <summary>
        /// Messages ready to be read.
        /// </summary>
        public Queue<MqMessage> Messages { get; }

        /// <summary>
        /// If this message is on the server, this will contain the reference to the connected session of the client.
        /// </summary>
        public TSession Session { get; }

        /// <summary>
        /// Creates an instance of the event args.
        /// </summary>
        /// <param name="messages">Messages read and ready to be used.</param>
        /// <param name="session">Server session.  Null if this is on the client.</param>
        public IncomingMessageEventArgs(Queue<MqMessage> messages, TSession session)
        {
            Messages = messages;
            Session = session;
        }
    }
}