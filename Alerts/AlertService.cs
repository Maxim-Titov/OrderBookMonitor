namespace OrderBookMonitor.Alerts
{
    public sealed class AlertService
    {
        private readonly List<IAlertChannel> _channels = new();

        public AlertService(IEnumerable<IAlertChannel> channels)
        {
            _channels.AddRange(channels);
        }

        public async Task PublishAsync(Alert alert)
        {
            foreach (var channel in _channels)
            {
                await channel.SendAsync(alert);
            }
        }
    }
}
