namespace OrderBookMonitor.Alerts
{
    public sealed class Alert
    {
        public string Title { get; init; } = "";
        public string Message { get; init; } = "";
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
