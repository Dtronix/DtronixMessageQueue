using System;
using System.Buffers;
using System.Data;
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
        private TlsTaskScheduler _taskScheduler;
        private TlsInnerStream _innerStream;
        private SslStream _tlsStream;

        private IMemoryOwner<byte> _tlsReadBuffer;
        private X509Certificate _sessionCertificate;

        //private SemaphoreSlim _authSemaphore = new SemaphoreSlim(0, 1);
        private CancellationTokenSource _cancellationTokenSource;
        private readonly int _bufferSize;

        public TlsApplicationSession(ITransportSession transportSession, TlsApplicationConfig config, BufferMemoryPool memoryPool, TlsTaskScheduler taskScheduler)
        :base(transportSession, config)
        {
            _config = config;
            _taskScheduler = taskScheduler;
            _innerStream = new TlsInnerStream(OnTlsStreamWrite);
            _tlsStream = new SslStream(_innerStream, true, _config.CertificateValidationCallback);
            _tlsReadBuffer = memoryPool.Rent();
            _bufferSize = _tlsReadBuffer.Memory.Length;
            _cancellationTokenSource = new CancellationTokenSource(_config.AuthTimeout);

        }

        protected override void OnTransportSessionConnected(object sender, SessionEventArgs e)
        {
            Task.Factory.StartNew(StartSession, _taskScheduler);

            // Alert that we are connected, but not ready.
            base.OnTransportSessionConnected(this, new SessionEventArgs(this));
        }


        protected override void OnTransportSessionReady(object sender, SessionEventArgs e)
        {
            // Do nothing as at this point, we are not authenticated.
        }

        private async void StartSession(object obj)
        {
            try
            {
                if (TransportSession.Mode == SessionMode.Client)
                {
                    await _tlsStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                        {
                            TargetHost = "tlstest" // TODO: Change!
                        },
                        _cancellationTokenSource.Token);
                }
                else
                {
                    await _tlsStream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions()
                    {
                        ServerCertificate = _config.Certificate
                    }, _cancellationTokenSource.Token);
                }
            }
            catch (Exception e)
            {
               _config.Logger.Warn($"{Mode} TlsApplication authentication failure. {e}");
            }

            if (!_tlsStream.IsAuthenticated)
            {
                Disconnect();
                return;
            }

            _config.Logger?.Trace($"{Mode} TlsApplication authentication: {_tlsStream.IsAuthenticated}");
            
            // Only pass on the ready status if the authentication is successful.
            if(_tlsStream.IsAuthenticated)
                base.OnTransportSessionReady(this, new SessionEventArgs(this));

            try
            {
                var len = 0;
                while ((len = await _tlsStream.ReadAsync(_tlsReadBuffer.Memory, _cancellationTokenSource.Token)) != 0)
                {
                    base.OnSessionReceive(_tlsReadBuffer.Memory.Slice(0, len));
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
            catch (Exception e)
            {
                _config.Logger?.Error($"{Mode} TlsApplication read exception: {e}");
            }


        }

        protected override void OnTransportSessionDisconnected(object sender, SessionEventArgs e)
        {
            _cancellationTokenSource.Cancel();
            base.OnTransportSessionDisconnected(sender, e);
        }

        protected override void OnSessionReceive(ReadOnlyMemory<byte> buffer)
        {
            _config.Logger?.Trace($"{Mode} TlsApplication read {buffer.Length} encrypted bytes.");
            var read = _innerStream.AsyncReadReceived(buffer, _cancellationTokenSource.Token);

            try
            {
                // Block further reads until the entire buffer has been read.
                read.Wait(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
            catch (Exception e)
            {
                _config.Logger?.Trace($"{Mode} TlsApplication exception occured while receiving. {e}");
                throw;
            }

        }

        public override void Send(ReadOnlyMemory<byte> buffer, bool flush)
        {
            if (buffer.Length > _bufferSize)
            {

                _config.Logger?.Error(
                    $"{Mode} TlsApplication Sending {buffer.Length} bytes exceeds the SendAndReceiveBufferSize[{_bufferSize}].");
                throw new Exception(
                    $"{Mode} TlsApplication Sending {buffer.Length} bytes exceeds the SendAndReceiveBufferSize[{_bufferSize}].");

            }

            _tlsStream.Write(buffer.Span);

            if(flush)
                _tlsStream.Flush();

            if (LastSendException != null)
                throw LastSendException;

        }


        private void OnTlsStreamWrite(ReadOnlyMemory<byte> memory)
        {
            base.Send(memory, true);
        }

    }
}
