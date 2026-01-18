using OrderBookMonitor.OrderBook;

namespace OrderBookMonitor.Alerts
{
    public sealed class SpreadSpikeRule : ISignalRule
    {
        private readonly decimal _absoluteThreshold;
        private readonly decimal _multiplier;
        private readonly TimeSpan _cooldown;

        private decimal _emaSpread = 0m;
        private DateTime _lastAlertTime = DateTime.MinValue;

        private const decimal Alpha = 0.1m; // EMA smoothing

        public SpreadSpikeRule(
            decimal absoluteThreshold,
            decimal multiplier,
            TimeSpan cooldown)
        {
            _absoluteThreshold = absoluteThreshold;
            _multiplier = multiplier;
            _cooldown = cooldown;
        }

        public Alert? Evaluate(OrderBookSnapshot snapshot)
        {
            if (snapshot.Bids.Count == 0 || snapshot.Asks.Count == 0)
                return null;

            var bestBid = snapshot.Bids[0].Price;
            var bestAsk = snapshot.Asks[0].Price;

            var spread = bestAsk - bestBid;
            if (spread <= 0)
                return null;

            // ===== UPDATE BASELINE =====

            if (_emaSpread == 0m)
                _emaSpread = spread;
            else
                _emaSpread = Alpha * spread + (1 - Alpha) * _emaSpread;

            var now = DateTime.UtcNow;

            bool absoluteSpike = spread >= _absoluteThreshold;
            bool relativeSpike = spread >= _emaSpread * _multiplier;
            bool inCooldown = now - _lastAlertTime < _cooldown;

            if (inCooldown)
                return null;

            if (!absoluteSpike && !relativeSpike)
                return null;

            _lastAlertTime = now;

            return new Alert
            {
                Title = $"⚠️ Spread spike {snapshot.Symbol}",
                Message =
                    $"Spread: {spread:F2} USDT\n" +
                    $"Normal: {_emaSpread:F2} USDT"
            };
        }
    }
}
