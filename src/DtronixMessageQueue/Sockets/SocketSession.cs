using System;
using System.Collections.Generic;
using System.Text;
using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.Sockets
{
    public class SocketSession : ISession
    {
        protected readonly ISession Session;

        public Action<ReadOnlyMemory<byte>> Received { get; set; }
        public Action<ISession> Sent { get; set; }
        public event EventHandler<SessionEventArgs> Disconnected;
        public event EventHandler<SessionEventArgs> Connected;

        public SessionState State => Session.State;

        public SocketSession(ISession session)
        {
            Session = session;
            Session.Received = OnSessionReceive;
            Session.Sent = OnSessionSent;
            Session.Disconnected += OnSessionDisconnected;
            Session.Connected += OnSessionConnected;
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

        public virtual void Send(ReadOnlyMemory<byte> buffer)
        {
            Session.Send(buffer);
        }
    }
}
