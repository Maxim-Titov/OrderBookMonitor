using OrderBookMonitor.Networking;
using OrderBookMonitor.Subscriptions;
using OrderBookMonitor.Decoding;
using OrderBookMonitor.OrderBook;
using OrderBookMonitor.UI;
using OrderBookMonitor.Logging;
using OrderBookMonitor.Alerts;
using OrderBookMonitor.Infrastructure;

namespace OrderBookMonitor
{
    class MainClass
    {
        static async Task Main()
        {
            EnvLoader.Load();

            var ws = new MexcWebSocketClient("wss://wbs-api.mexc.com/ws");
            var subscriber = new MexcSubscriptionService(ws);
            var decoder = new MexcProtobufDecoder();
            var processor = new OrderBookProcessor();
            var renderer = new ConsoleOrderBookRenderer();
            var logger = new FileLogger();

            var botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            var chatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID");

            if (string.IsNullOrWhiteSpace(botToken))
            {
                logger.Error("TELEGRAM_BOT_TOKEN is not set");
                return;
            }

            if (string.IsNullOrWhiteSpace(chatId))
            {
                logger.Error("TELEGRAM_CHAT_ID is not set");
                return;
            }

            var telegram = new TelegramAlertChannel(botToken: botToken, chatId: chatId);

            var alertService = new AlertService(new[]
            {
                telegram
            });

            var signalEngine = new SignalEngine(
                new ISignalRule[]
                {
                    new LargeWallRule(minVolume: 10m, maxDistancePct: 0.1m, cooldown: TimeSpan.FromMinutes(2)),
                    new SpreadSpikeRule(absoluteThreshold: 10m, multiplier: 3m, cooldown: TimeSpan.FromMinutes(1))
                },
                alertService
            );

            logger.Info("Application started");

            _ = alertService.PublishAsync(new Alert
            {
                Title = "WhaleSniper online",
                Message = "Market feed connected 🚀"
            });

            ws.OnBinaryMessage += data =>
            {
                var book = decoder.Decode(data);
                if (book != null)
                {
                    processor.Apply(book);
                    renderer.Render(book);

                    // алерти не блокують стакан
                    _ = signalEngine.ProcessAsync(book);
                }
            };

            ws.Connect();
            await subscriber.SubscribeDepthAsync("BTCUSDT", 20);

            await Task.Delay(Timeout.Infinite);
        }
    }
}
