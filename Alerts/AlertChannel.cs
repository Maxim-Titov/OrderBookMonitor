using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrderBookMonitor.Logging;

namespace OrderBookMonitor.Alerts
{
    public sealed class TelegramAlertChannel : IAlertChannel, IDisposable
    {
        private readonly string _botToken;
        private readonly string _chatId;
        private readonly HttpClient _http;
        private readonly FileLogger _logger = new();

        private readonly Uri _sendMessageUri;

        public TelegramAlertChannel(string botToken, string chatId)
        {
            _botToken = botToken;
            _chatId = chatId;

            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            _sendMessageUri =
                new Uri($"https://api.telegram.org/bot{_botToken}/sendMessage");
        }

        public async Task SendAsync(Alert alert)
        {
            try
            {
                var payload = new TelegramSendMessageRequest
                {
                    ChatId = _chatId,
                    Text = Format(alert),
                    ParseMode = "HTML",
                    DisableWebPagePreview = true
                };

                using var response = await _http.PostAsJsonAsync(_sendMessageUri, payload);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.Info($"Telegram API error: {response.StatusCode} {body}");
                }
            }
            catch (TaskCanceledException)
            {
                _logger.Info("Telegram timeout");
            }
            catch (Exception ex)
            {
                _logger.Info($"Telegram send failed: {ex.Message}");
            }
        }

        private static string Format(Alert alert)
        {
            return
                $"<b>{Escape(alert.Title)}</b>\n\n" +
                $"{Escape(alert.Message)}\n\n" +
                $"<i>{alert.Timestamp:yyyy-MM-dd HH:mm:ss} UTC</i>";
        }

        private static string Escape(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        public void Dispose()
        {
            _http.Dispose();
        }

        // ===== DTO =====

        private sealed class TelegramSendMessageRequest
        {
            [JsonPropertyName("chat_id")]
            public string ChatId { get; init; } = "";

            [JsonPropertyName("text")]
            public string Text { get; init; } = "";

            [JsonPropertyName("parse_mode")]
            public string ParseMode { get; init; } = "HTML";

            [JsonPropertyName("disable_web_page_preview")]
            public bool DisableWebPagePreview { get; init; } = true;
        }
    }
}
