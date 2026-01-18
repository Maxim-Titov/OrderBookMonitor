namespace OrderBookMonitor.OrderBook;

public record OrderBookLevel(decimal Price, decimal Quantity);

public class OrderBookSnapshot
{
    public string Symbol { get; init; } = "";
    public long Version { get; init; }
    public List<OrderBookLevel> Bids { get; init; } = [];
    public List<OrderBookLevel> Asks { get; init; } = [];
}
