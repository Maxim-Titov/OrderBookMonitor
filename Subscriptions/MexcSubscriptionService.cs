using System.Collections.Concurrent;
using System.Text.Json;
using OrderBookMonitor.Networking;
using OrderBookMonitor.Logging;

namespace OrderBookMonitor.Subscriptions
{
    public sealed class MexcSubscriptionService
    {
        private readonly MexcWebSocketClient _ws;
        private readonly FileLogger _logger = new();

        private readonly ConcurrentDictionary<string, string> _subscriptions = new();

        public MexcSubscriptionService(MexcWebSocketClient ws)
        {
            _ws = ws;
            _ws.Connected += HandleConnected;
        }

        // ===== PUBLIC API =====

        public async Task SubscribeDepthAsync(string symbol, int depth)
        {
            var topic = $"spot@public.limit.depth.v3.api.pb@{symbol}@{depth}";

            _subscriptions[topic] = topic;

            await SendSubscribeAsync(topic);

            _logger.Info($"Subscribed to {symbol} depth {depth}");
        }

        // ===== RESUBSCRIBE AFTER RECONNECT =====

        private async Task HandleConnected()
        {
            _logger.Info("Resubscribing after reconnect...");

            foreach (var topic in _subscriptions.Keys)
            {
                await SendSubscribeAsync(topic);
            }
        }

        // ===== INTERNAL SEND =====

        private async Task SendSubscribeAsync(string topic)
        {
            var payload = new
            {
                method = "SUBSCRIPTION",
                @params = new[] { topic }
            };

            var json = JsonSerializer.Serialize(payload);

            await _ws.SendAsync(json);
        }
    }
}
