using OrderBookMonitor.OrderBook;

namespace OrderBookMonitor.Alerts
{
    public sealed class SignalEngine
    {
        private readonly List<ISignalRule> _rules = new();
        private readonly AlertService _alerts;

        public SignalEngine(IEnumerable<ISignalRule> rules, AlertService alerts)
        {
            _rules.AddRange(rules);
            _alerts = alerts;
        }

        public async Task ProcessAsync(OrderBookSnapshot snapshot)
        {
            foreach (var rule in _rules)
            {
                var alert = rule.Evaluate(snapshot);
                if (alert != null)
                {
                    await _alerts.PublishAsync(alert);
                }
            }
        }
    }
}
