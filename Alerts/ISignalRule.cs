using OrderBookMonitor.OrderBook;

namespace OrderBookMonitor.Alerts
{
    public interface ISignalRule
    {
        Alert? Evaluate(OrderBookSnapshot snapshot);
    }
}
