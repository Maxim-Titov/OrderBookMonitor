using System.Globalization;
using Google.Protobuf;
using OrderBookMonitor.OrderBook;
using OrderBookMonitor.Logging;

namespace OrderBookMonitor.Decoding
{
    public class MexcProtobufDecoder
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public OrderBookSnapshot? Decode(byte[] data)
        {
            var logger = new FileLogger();

            try
            {
                var wrapper = new PushDataV3ApiWrapper();
                wrapper.MergeFrom(data);

                if (wrapper.BodyCase !=
                    PushDataV3ApiWrapper.BodyOneofCase.PublicLimitDepths)
                    return null;

                var book = wrapper.PublicLimitDepths;

                return new OrderBookSnapshot
                {
                    Symbol = wrapper.Symbol,

                    // string -> long
                    Version = long.Parse(book.Version, Invariant),

                    Bids = book.Bids
                        .Select(b => new OrderBookLevel(
                            ParseDecimal(b.Price),
                            ParseDecimal(b.Quantity)
                        ))
                        .ToList(),

                    Asks = book.Asks
                        .Select(a => new OrderBookLevel(
                            ParseDecimal(a.Price),
                            ParseDecimal(a.Quantity)
                        ))
                        .ToList()
                };
            }
            catch (Exception e)
            {
                logger.Error(e);
                return null;
            }
        }

        private static decimal ParseDecimal(string value)
        {
            return decimal.Parse(value, Invariant);
        }
    }
}
