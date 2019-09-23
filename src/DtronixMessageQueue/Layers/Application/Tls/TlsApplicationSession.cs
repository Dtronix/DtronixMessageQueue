using System;
using System.Buffers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
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
        private X509Certificate _sessionCertificate;

        private SemaphoreSlim _authSemaphore = new SemaphoreSlim(0, 1);
        private CancellationTokenSource _authSemaphoreCancellationTokenSource;

        public TlsApplicationSession(ITransportSession transportSession, TlsApplicationConfig config, BufferMemoryPool memoryPool, TlsAuthScheduler scheduler)
        :base(transportSession, config)
        {
            _config = config;
            _scheduler = scheduler;
            _innerStream = new TlsInnerStream(OnTlsStreamWrite);
            _tlsStream = new SslStream(_innerStream, true, _config.CertificateValidationCallback);
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
            _authSemaphoreCancellationTokenSource = new CancellationTokenSource(_config.AuthTimeout);
            try
            {
                if (TransportSession.Mode == SessionMode.Client)
                    await _tlsStream.AuthenticateAsClientAsync("tlstest"); // TODO: Change!
                else
                    await _tlsStream.AuthenticateAsServerAsync(_config.Certificate);
            }
            catch (Exception e)
            {
               _config.Logger.Warn($"{Mode} TlsApplication authentication failure. {e}");
            }

            // Allow reading of data now that the authentication process has completed.
            _authSemaphore.Release();

            _config.Logger?.Trace($"{Mode} TlsApplication authentication: {_tlsStream.IsAuthenticated}");
            
            // Only pass on the ready status if the authentication is successful.
            if(_tlsStream.IsAuthenticated)
                base.OnTransportSessionReady(this, new SessionEventArgs(this));
        }

        protected override void OnTransportSessionDisconnected(object sender, SessionEventArgs e)
        {
            _authSemaphore?.Release();
            base.OnTransportSessionDisconnected(sender, e);
        }

        protected override async void OnSessionReceive(ReadOnlyMemory<byte> buffer)
        {
            _config.Logger?.Trace($"{Mode} TlsApplication read {buffer.Length} encrypted bytes.");

            _innerStream.Received(buffer, _authSemaphoreCancellationTokenSource.Token);

            // If there is not a wait on the inner stream pending, this will mean that the reads are complete
            // and the authentication process is in progress.  We need to wait for this process to complete
            // before we can read data.
            // TODO: Tests for deadlocks.
            if (_authSemaphore != null 
                && !_tlsStream.IsAuthenticated 
                && !_innerStream.IsReadWaiting)
            {
                _config.Logger?.Trace($"{Mode} TlsApplication authentication in process of completing.  Semaphore waiting...");
                try
                {
                    _authSemaphore.Wait(_authSemaphoreCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    
                    _config.Logger?.Trace($"{Mode} TlsApplication authentication timed out.");
                    Disconnect();
                }

                _authSemaphore.Dispose();
                _authSemaphore = null;
                _config.Logger?.Trace($"{Mode} TlsApplication authentication semaphore released.");
            }

            // If we are not authenticated, then this data should be relegated to the auth process only.
            if (!_tlsStream.IsAuthenticated)
            {
                _config.Logger?.Trace($"{Mode} TlsApplication not authenticated.");
                return;
            }

            // This can block...
            var read = await _tlsStream.ReadAsync(_tlsReadBuffer.Memory);

            _config.Logger?.Trace($"{Mode} TlsApplication read {read} clear bytes.");

            if (read > 0)
                base.OnSessionReceive(_tlsReadBuffer.Memory.Slice(0, read));
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
