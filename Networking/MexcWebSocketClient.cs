using System.Net.WebSockets;
using System.Text;
using OrderBookMonitor.Logging;

namespace OrderBookMonitor.Networking
{
    public sealed class MexcWebSocketClient : IDisposable
    {
        private readonly Uri _uri;
        private readonly FileLogger _logger = new();

        private ClientWebSocket? _socket;
        private CancellationTokenSource? _cts;
        private Task? _runner;

        private readonly object _sync = new();

        public event Action<byte[]>? OnBinaryMessage;
        public event Func<Task>? Connected;

        public MexcWebSocketClient(string url)
        {
            _uri = new Uri(url);
        }

        // ===== PUBLIC API =====

        public void Connect()
        {
            lock (_sync)
            {
                if (_runner != null)
                    return;

                _cts = new CancellationTokenSource();
                _runner = Task.Run(RunAsync);
            }
        }

        public async Task SendAsync(string json)
        {
            ClientWebSocket? socket;

            lock (_sync)
                socket = _socket;

            if (socket == null || socket.State != WebSocketState.Open)
                return;

            var bytes = Encoding.UTF8.GetBytes(json);
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, _cts!.Token);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _runner?.Wait(2000);
            CleanupSocket();
        }

        // ===== CORE LOOP =====

        private async Task RunAsync()
        {
            while (!_cts!.IsCancellationRequested)
            {
                try
                {
                    await ConnectInternalAsync();
                    await ReceiveLoopAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Info($"WebSocket error: {ex.Message}");
                }

                _logger.Info("Disconnected. Reconnecting in 3s...");
                CleanupSocket();

                await Task.Delay(3000, _cts.Token);
            }
        }

        // ===== CONNECT =====

        private async Task ConnectInternalAsync()
        {
            CleanupSocket();

            var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);

            await socket.ConnectAsync(_uri, _cts!.Token);

            lock (_sync)
                _socket = socket;

            _logger.Info("Connected to MEXC");

            if (Connected != null)
                await Connected.Invoke();
        }

        // ===== RECEIVE LOOP =====

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[8192];

            while (true)
            {
                ClientWebSocket? socket;

                lock (_sync)
                    socket = _socket;

                if (socket == null || socket.State != WebSocketState.Open)
                    break;

                using var ms = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await socket.ReceiveAsync(buffer, _cts!.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                        throw new WebSocketException("Server closed connection");

                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    OnBinaryMessage?.Invoke(ms.ToArray());
                }
            }
        }

        // ===== CLEANUP =====

        private void CleanupSocket()
        {
            ClientWebSocket? socket;

            lock (_sync)
            {
                socket = _socket;
                _socket = null;
            }

            try
            {
                if (socket != null)
                {
                    if (socket.State == WebSocketState.Open ||
                        socket.State == WebSocketState.CloseReceived)
                    {
                        socket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closing",
                            CancellationToken.None
                        ).Wait(1000);
                    }

                    socket.Dispose();
                }
            }
            catch { }
        }
    }
}
