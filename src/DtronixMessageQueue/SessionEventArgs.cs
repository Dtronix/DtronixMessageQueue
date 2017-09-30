using System;

namespace DtronixMessageQueue
{
    /// <summary>
    /// Event args used when the session has connected to a remote endpoint.
    /// </summary>
    /// <typeparam name="TSession">Session type for this connection.</typeparam>
    /// <typeparam name="TConfig">Configuration for this connection.</typeparam>
    public class SessionEventArgs<TSession, TConfig> : EventArgs
        where TSession : MqSession<TSession, TConfig>, new()
        where TConfig : MqConfig
    {
        /// <summary>
        /// Connected session.
        /// </summary>
        public TSession Session { get; }

        /// <summary>
        /// Creates a new instance of the session connected event args.
        /// </summary>
        /// <param name="session">Connected session.</param>
        public SessionEventArgs(TSession session)
        {
            Session = session;
        }
    }
}