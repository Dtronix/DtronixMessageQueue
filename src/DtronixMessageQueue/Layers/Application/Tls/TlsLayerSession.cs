using System;
using System.Buffers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace DtronixMessageQueue.Layers.Application.Tls
{
    public class TlsLayerSession : ApplicationSession
    {
        private readonly TlsLayerConfig _config;
        private TlsAuthScheduler _scheduler;
        private TlsInnerStream _innerStream;
        private SslStream _tlsStream;

        private IMemoryOwner<byte> _tlsReadBuffer;
        private IMemoryOwner<byte> _tlsWriteBuffer;
        private X509Certificate _sessionCertificate;

        public TlsLayerSession(ISession session, TlsLayerConfig config, BufferMemoryPool memoryPool, TlsAuthScheduler scheduler)
        :base(session)
        {
            _config = config;
            _scheduler = scheduler;
            _innerStream = new TlsInnerStream(OnTlsStreamWrite);
            _tlsStream = new SslStream(_innerStream, true);
            _tlsWriteBuffer = memoryPool.Rent();
            _tlsReadBuffer = memoryPool.Rent();

        }

        protected override void OnSessionConnected(object sender, SessionEventArgs e)
        {
            Task.Factory.StartNew(AuthenticateSession, _scheduler);
            // Do not pass on the session connected event until authentication has occured.
        }

        private async void AuthenticateSession(object obj)
        {
            if (Session.Mode == SessionMode.Client)
                await _tlsStream.AuthenticateAsClientAsync("tlstest"); // TODO: Change!
            else
                await _tlsStream.AuthenticateAsServerAsync(_config.Certificate);
                    
            base.OnSessionConnected(this, new SessionEventArgs(this));
        }

        protected override async void OnSessionReceive(ReadOnlyMemory<byte> buffer)
        {
            _innerStream.Received(buffer);

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
