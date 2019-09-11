using System;
using System.Collections.Generic;
using System.Text;
using DtronixMessageQueue.Transports;

namespace DtronixMessageQueue.Sockets
{
    public class SocketListener : IListener
    {
        protected readonly IListener Listener;

        public Action<ISession> Connected { get; set; }
        public Action<ISession> Disconnected { get; set; }

        public event EventHandler Stopped;
        public event EventHandler Started;

        public bool IsListening => Listener.IsListening;

        public SocketListener(ITransportFactory factory)
        {
            Listener = factory.CreateListener(OnSessionCreated);

            Listener.Connected = OnConnected;
            Listener.Disconnected = OnDisconnected;
            Listener.Started += OnListenerStarted;
            Listener.Stopped += OnListenerStopped;
        }

        protected virtual void OnSessionCreated(ISession session)
        {
            if (session is ITransportSession transportSession)
            {
                // Set the wrapper session to this new socket session.
                transportSession.WrapperSession = new SocketSession(transportSession);
            }
        }

        protected  virtual void OnListenerStarted(object sender, EventArgs e)
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnListenerStopped(object sender, EventArgs e)
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected(ISession session)
        {
            if (session is ITransportSession transportSession)
            {
                Disconnected?.Invoke(transportSession.WrapperSession);
            }
        }

        protected virtual void OnConnected(ISession session)
        {
            if (session is ITransportSession transportSession)
            {
                Connected?.Invoke(transportSession.WrapperSession);
            }
        }

        public void Start()
        {
            Listener.Start();
        }

        public void Stop()
        {
            Listener.Stop();
        }
    }
}
