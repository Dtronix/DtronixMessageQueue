using System;
using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application
{
    public abstract class ApplicationSession : ISession
    {
        public ITransportSession TransportSession { get; }

        public SessionMode Mode { get; }

        public Action<ReadOnlyMemory<byte>> Received { get; set; }
        public Action<ISession> Sent { get; set; }
        public event EventHandler<SessionEventArgs> Disconnected;

        public event EventHandler<SessionEventArgs> Connected;

        public SessionState State => TransportSession.State;

        protected ApplicationSession(ITransportSession transportSession)
        {
            TransportSession = transportSession;
            TransportSession.Received = OnSessionReceive;
            TransportSession.Sent = OnSessionSent;
            TransportSession.Disconnected += OnTransportSessionDisconnected;
            TransportSession.Connected += OnTransportSessionConnected;

            Mode = transportSession.Mode;
        }

        protected virtual void OnTransportSessionConnected(object sender, SessionEventArgs e)
        {
            Connected?.Invoke(this, new SessionEventArgs(this));
        }

        protected virtual void OnTransportSessionDisconnected(object sender, SessionEventArgs e)
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
            TransportSession.Received = null;
            TransportSession.Sent = null;

            TransportSession.Disconnect();
        }

        public virtual void Send(ReadOnlyMemory<byte> buffer, bool flush)
        {
            TransportSession.Send(buffer, flush);
        }
    }
}
