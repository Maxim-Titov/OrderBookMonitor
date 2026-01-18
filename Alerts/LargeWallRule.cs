using OrderBookMonitor.OrderBook;

namespace OrderBookMonitor.Alerts
{
    public sealed class LargeWallRule : ISignalRule
    {
        private readonly decimal _minVolume;
        private readonly decimal _maxDistancePct;
        private readonly TimeSpan _cooldown;

        private WallState? _lastBidWall;
        private WallState? _lastAskWall;
        private DateTime _lastAlertTime = DateTime.MinValue;

        public LargeWallRule(
            decimal minVolume,
            decimal maxDistancePct,
            TimeSpan cooldown)
        {
            _minVolume = minVolume;
            _maxDistancePct = maxDistancePct;
            _cooldown = cooldown;
        }

        public Alert? Evaluate(OrderBookSnapshot snapshot)
        {
            var now = DateTime.UtcNow;

            var bestBid = snapshot.Bids.FirstOrDefault();
            var bestAsk = snapshot.Asks.FirstOrDefault();

            if (bestBid == null || bestAsk == null)
                return null;

            var bidWall = snapshot.Bids
                .FirstOrDefault(b => b.Quantity >= _minVolume);

            var askWall = snapshot.Asks
                .FirstOrDefault(a => a.Quantity >= _minVolume);

            Alert? alert = null;

            if (bidWall != null)
            {
                alert = EvaluateWall(
                    side: "🟢 BID WALL",
                    wall: bidWall,
                    referencePrice: bestBid.Price,
                    lastState: ref _lastBidWall,
                    snapshot.Symbol,
                    now);
            }

            if (alert == null && askWall != null)
            {
                alert = EvaluateWall(
                    side: "🔴 ASK WALL",
                    wall: askWall,
                    referencePrice: bestAsk.Price,
                    lastState: ref _lastAskWall,
                    snapshot.Symbol,
                    now);
            }

            if (alert != null)
                _lastAlertTime = now;

            return alert;
        }

        private Alert? EvaluateWall(
            string side,
            OrderBookLevel wall,
            decimal referencePrice,
            ref WallState? lastState,
            string symbol,
            DateTime now)
        {
            var distancePct =
                Math.Abs(wall.Price - referencePrice) / referencePrice * 100m;

            // Стінка занадто далеко від ціни — неважлива
            if (distancePct > _maxDistancePct)
                return null;

            if (now - _lastAlertTime < _cooldown)
                return null;

            if (lastState == null)
            {
                lastState = new WallState(wall.Price, wall.Quantity);
                return CreateAlert("APPEARED", side, wall, symbol);
            }

            if (lastState.Price != wall.Price)
            {
                lastState = new WallState(wall.Price, wall.Quantity);
                return CreateAlert("MOVED", side, wall, symbol);
            }

            var volumeDelta = wall.Quantity - lastState.Quantity;

            if (Math.Abs(volumeDelta) / lastState.Quantity >= 0.3m)
            {
                lastState = new WallState(wall.Price, wall.Quantity);
                return CreateAlert(
                    volumeDelta > 0 ? "STRENGTHENED" : "WEAKENED",
                    side, wall, symbol);
            }

            lastState = new WallState(wall.Price, wall.Quantity);
            return null;
        }

        private static Alert CreateAlert(
            string eventType,
            string side,
            OrderBookLevel wall,
            string symbol)
        {
            return new Alert
            {
                Title = $"{side} {eventType} {symbol}",
                Message =
                    $"Price: {wall.Price}\n" +
                    $"Qty: {wall.Quantity}"
            };
        }

        private sealed record WallState(decimal Price, decimal Quantity);
    }
}
