using System;

namespace DtronixMessageQueue.Layers.Application
{
    public class ApplicationSession : ISession
    {
        protected readonly ISession Session;

        public SessionMode Mode { get; }

        public Action<ReadOnlyMemory<byte>> Received { get; set; }
        public Action<ISession> Sent { get; set; }
        public event EventHandler<SessionEventArgs> Disconnected;

        public event EventHandler<SessionEventArgs> Connected;

        public SessionState State => Session.State;

        public ApplicationSession(ISession session)
        {
            Session = session;
            Session.Received = OnSessionReceive;
            Session.Sent = OnSessionSent;
            Session.Disconnected += OnSessionDisconnected;
            Session.Connected += OnSessionConnected;

            Mode = session.Mode;
        }

        protected virtual void OnSessionConnected(object sender, SessionEventArgs e)
        {
            Connected?.Invoke(this, new SessionEventArgs(this));
        }

        protected virtual void OnSessionDisconnected(object sender, SessionEventArgs e)
        {
            Disconnected?.Invoke(this, new SessionEventArgs(this));
        }

        protected virtual void OnSessionReceive(ReadOnlyMemory<byte> buffer)
        {
            Received?.Invoke(buffer);
        }

        protected virtual void OnSessionSent(ISession session)
        {
            Sent?.Invoke(this);
        }


        public virtual void Disconnect()
        {
            Session.Received = null;
            Session.Sent = null;

            Session.Disconnect();
        }

        public virtual void Send(ReadOnlyMemory<byte> buffer, bool flush)
        {
            Session.Send(buffer, flush);
        }
    }
}
