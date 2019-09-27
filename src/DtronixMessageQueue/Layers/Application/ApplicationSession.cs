﻿using System;
using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application
{
    public abstract class ApplicationSession : ISession
    {
        private readonly ApplicationConfig _config;
        public ITransportSession TransportSession { get; }

        public SessionMode Mode { get; }

        public Action<ReadOnlyMemory<byte>> Received { get; set; }
        public Action<ISession> Sent { get; set; }
        public event EventHandler<SessionEventArgs> Disconnected;

        public event EventHandler<SessionEventArgs> Connected;
        public event EventHandler<SessionEventArgs> Ready;

        public SessionState State => TransportSession.State;

        protected Exception LastSendException;

        private int _bufferSize;

        protected ApplicationSession(ITransportSession transportSession, ApplicationConfig config)
        {
            _config = config;
            TransportSession = transportSession;
            TransportSession.Received = OnSessionReceive;
            TransportSession.Sent = OnSessionSent;
            TransportSession.Disconnected += OnTransportSessionDisconnected;
            TransportSession.Connected += OnTransportSessionConnected;
            TransportSession.Ready += OnTransportSessionReady;

            Mode = transportSession.Mode;

            _bufferSize = _config.TransportConfig.SendAndReceiveBufferSize;
        }

        protected virtual void OnTransportSessionReady(object sender, SessionEventArgs e)
        {
            _config.Logger?.Trace($"{Mode} Application Ready.");
            Ready?.Invoke(this, new SessionEventArgs(this));
        }

        protected virtual void OnTransportSessionConnected(object sender, SessionEventArgs e)
        {
            _config.Logger?.Trace($"{Mode} Application Connected.");
            Connected?.Invoke(this, new SessionEventArgs(this));
        }

        protected virtual void OnTransportSessionDisconnected(object sender, SessionEventArgs e)
        {
            _config.Logger?.Trace($"{Mode} Application Disconnected.");

            TransportSession.Received = null;
            TransportSession.Sent = null;

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
            TransportSession.Disconnect();
        }

        public virtual void Send(ReadOnlyMemory<byte> buffer, bool flush)
        {
            if (buffer.Length > _bufferSize)
            {

                _config.Logger?.Error(
                    $"{Mode} TlsApplication Sending {buffer.Length} bytes exceeds the SendAndReceiveBufferSize[{_bufferSize}].");
                throw new Exception(
                    $"{Mode} TlsApplication Sending {buffer.Length} bytes exceeds the SendAndReceiveBufferSize[{_bufferSize}].");

            }

            _config.Logger?.Trace($"{Mode} Application sent {buffer.Length} bytes. Flush: {flush}");
            try
            {
                TransportSession.Send(buffer, flush);
            }
            catch (Exception e)
            {
                LastSendException = e;
            }
            
        }
    }
}
