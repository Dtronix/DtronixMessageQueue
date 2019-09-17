using System;
using System.Diagnostics.CodeAnalysis;
using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application
{
    public abstract class ApplicationListener : IListener
    {
        protected readonly IListener Listener;

        public event EventHandler<SessionEventArgs> Connected;
        public event EventHandler<SessionEventArgs> Disconnected;

        public event EventHandler Stopped;
        public event EventHandler Started;

        public bool IsListening => Listener.IsListening;

        protected ApplicationListener(ITransportFactory factory)
        {
            Listener = factory.CreateListener(OnSessionCreated);
            Listener.Started += OnListenerStarted;
            Listener.Stopped += OnListenerStopped;
        }

        private void OnSessionCreated([NotNull] ITransportSession session)
        {
            var appSession = CreateSession(session);

            appSession.Connected += OnConnected;
            appSession.Disconnected += OnDisconnected;
        }

        protected virtual void OnDisconnected(object sender, SessionEventArgs e)
        {
            Disconnected?.Invoke(this, e);
        }

        protected virtual void OnConnected(object sender, SessionEventArgs e)
        {
            Connected?.Invoke(this, e);
        }

        protected abstract ApplicationSession CreateSession([NotNull] ITransportSession session);


        protected  virtual void OnListenerStarted(object sender, EventArgs e)
        {
            Started?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnListenerStopped(object sender, EventArgs e)
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        }

        public virtual void Start()
        {
            Listener.Start();
        }

        public virtual void Stop()
        {
            Listener.Stop();
        }
    }
}
