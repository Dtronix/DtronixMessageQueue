using System;
using System.Buffers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using DtronixMessageQueue.Layers.Transports;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsApplicationSession : ApplicationSession
    {
        private readonly TlsApplicationConfig _config;
        private TlsAuthScheduler _scheduler;
        private TlsInnerStream _innerStream;
        private SslStream _tlsStream;

        private IMemoryOwner<byte> _tlsReadBuffer;
        private IMemoryOwner<byte> _tlsWriteBuffer;
        private X509Certificate _sessionCertificate;

        public TlsApplicationSession(ITransportSession transportSession, TlsApplicationConfig config, BufferMemoryPool memoryPool, TlsAuthScheduler scheduler)
        :base(transportSession)
        {
            _config = config;
            _scheduler = scheduler;
            _innerStream = new TlsInnerStream(OnTlsStreamWrite);
            _tlsStream = new SslStream(_innerStream, true, _config.CertificateValidationCallback);
            _tlsWriteBuffer = memoryPool.Rent();
            _tlsReadBuffer = memoryPool.Rent();

        }

        protected override void OnTransportSessionConnected(object sender, SessionEventArgs e)
        {
            Task.Factory.StartNew(AuthenticateSession, _scheduler);

            // Alert that we are connected, but not ready.
            base.OnTransportSessionConnected(this, new SessionEventArgs(this));
        }

        protected override void OnTransportSessionReady(object sender, SessionEventArgs e)
        {
            // Do nothing as at this point, we are not authenticated.
        }

        private async void AuthenticateSession(object obj)
        {
            if (TransportSession.Mode == SessionMode.Client)
                await _tlsStream.AuthenticateAsClientAsync("tlstest"); // TODO: Change!
            else
                await _tlsStream.AuthenticateAsServerAsync(_config.Certificate);
                    

            base.OnTransportSessionReady(this, new SessionEventArgs(this));
        }

        protected override async void OnSessionReceive(ReadOnlyMemory<byte> buffer)
        {
            _innerStream.Received(buffer);

            // If we are not authenticated, then this data should be relegated to the auth process only.
            if (!_tlsStream.IsAuthenticated)
                return;

            // This can block...
            var read = await _tlsStream.ReadAsync(_tlsReadBuffer.Memory);

            if(read > 0)
                base.OnSessionReceive(_tlsReadBuffer.Memory);
        }

        public override void Send(ReadOnlyMemory<byte> buffer, bool flush)
        {
            _tlsStream.Write(buffer.Span);

            if(flush)
                _tlsStream.Flush();
        }


        private void OnTlsStreamWrite(ReadOnlyMemory<byte> memory)
        {
            base.Send(memory, true);
        }

    }
}
