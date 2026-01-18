namespace OrderBookMonitor.OrderBook;

public class OrderBookProcessor
{
    private readonly SortedDictionary<decimal, decimal> _bids = new();
    private readonly SortedDictionary<decimal, decimal> _asks = new();

    public void Apply(OrderBookSnapshot snapshot)
    {
        _bids.Clear();
        _asks.Clear();

        foreach (var b in snapshot.Bids)
            _bids[b.Price] = b.Quantity;

        foreach (var a in snapshot.Asks)
            _asks[a.Price] = a.Quantity;
    }
}
