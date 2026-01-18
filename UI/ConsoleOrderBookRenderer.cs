using System.Globalization;
using OrderBookMonitor.OrderBook;

namespace OrderBookMonitor.UI
{
    public class ConsoleOrderBookRenderer
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private const int IndexWidth = 3;
        private const int PriceWidth = 11;
        private const int QtyWidth = 10;

        private const int BlockWidth = PriceWidth + 1 + QtyWidth;
        private const int FullLeftWidth = IndexWidth + 3 + BlockWidth; // " # | "

        public void Render(OrderBookSnapshot book)
        {
            Console.Clear();

            RenderHeader(book);
            RenderTopLiquidity(book);
            RenderTableHeader();
            RenderRows(book);
            RenderHelp();
        }

        private static void RenderHeader(OrderBookSnapshot book)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"SYMBOL: {book.Symbol,-10}   VERSION: {book.Version}");
            Console.WriteLine();
        }

        private static void RenderTopLiquidity(OrderBookSnapshot book)
        {
            if (book.Bids.Count == 0 || book.Asks.Count == 0)
                return;

            var mid = (book.Bids[0].Price + book.Asks[0].Price) / 2m;

            var levels =
                book.Bids.Select(b => new
                {
                    Side = "BID",
                    b.Price,
                    b.Quantity,
                    Distance = Math.Abs(b.Price - mid) / mid * 100m
                })
                .Concat(book.Asks.Select(a => new
                {
                    Side = "ASK",
                    a.Price,
                    a.Quantity,
                    Distance = Math.Abs(a.Price - mid) / mid * 100m
                }))
                .Select(l => new
                {
                    l.Side,
                    l.Price,
                    l.Quantity,
                    l.Distance,
                    Score = l.Quantity / (l.Distance + 0.0001m)
                })
                .OrderByDescending(l => l.Score)
                .Take(5)
                .ToList();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TOP LIQUIDITY LEVELS");
            Console.ResetColor();

            foreach (var l in levels)
            {
                Console.WriteLine(
                    $"{l.Side,-3} {l.Price,10:F2}  {l.Quantity,8:F3}  {l.Distance,6:F3}%"
                );
            }

            Console.WriteLine();
        }

        private static void RenderTableHeader()
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine(
                $"{new string(' ', IndexWidth)} | " +
                $"{Center("BIDS", BlockWidth)} | {Center("ASKS", BlockWidth)}"
            );

            Console.WriteLine(new string('-', FullLeftWidth + 3 + BlockWidth));

            string bidHeader =
                $"{Center("PRICE", PriceWidth)} {Center("QTY", QtyWidth)}";

            string askHeader =
                $"{Center("PRICE", PriceWidth)} {Center("QTY", QtyWidth)}";

            Console.WriteLine(
                $"{Pad("#", IndexWidth)} | {bidHeader} | {askHeader}"
            );

            Console.WriteLine(new string('-', FullLeftWidth + 3 + BlockWidth));
        }

        private static void RenderRows(OrderBookSnapshot book)
        {
            int rows = Math.Max(book.Bids.Count, book.Asks.Count);

            var avgBidQty = book.Bids.Average(b => b.Quantity);
            var avgAskQty = book.Asks.Average(a => a.Quantity);

            for (int i = 0; i < rows; i++)
            {
                var bid = i < book.Bids.Count ? book.Bids[i] : null;
                var ask = i < book.Asks.Count ? book.Asks[i] : null;

                // index
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write((i + 1).ToString().PadLeft(IndexWidth));
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" | ");

                // ===== BIDS =====
                if (bid != null)
                {
                    if (bid.Quantity >= avgBidQty * 3)
                        Console.ForegroundColor = ConsoleColor.Yellow; // whale bid
                    else if (i == 0)
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    else
                        Console.ForegroundColor = ConsoleColor.Green;

                    Console.Write($"{FormatPrice(bid.Price)} {FormatQty(bid.Quantity)}");
                }
                else
                {
                    Console.Write($"{Blank(PriceWidth)} {Blank(QtyWidth)}");
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(" | ");

                // ===== ASKS =====
                if (ask != null)
                {
                    if (ask.Quantity >= avgAskQty * 3)
                        Console.ForegroundColor = ConsoleColor.Yellow; // whale ask
                    else if (i == 0)
                        Console.ForegroundColor = ConsoleColor.Magenta;
                    else
                        Console.ForegroundColor = ConsoleColor.Red;     // normal ask

                    Console.Write($"{FormatPrice(ask.Price)} {FormatQty(ask.Quantity)}");
                }
                else
                {
                    Console.Write($"{Blank(PriceWidth)} {Blank(QtyWidth)}");
                }

                Console.WriteLine();
            }

            Console.ResetColor();
        }

        private static void RenderHelp()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("LEGEND");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("■ ");
            Console.ResetColor();
            Console.Write("Best Bid");

            Console.Write("   ");

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write("■ ");
            Console.ResetColor();
            Console.Write("Best Ask");

            Console.Write("   ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("■ ");
            Console.ResetColor();
            Console.Write("Whale Wall (large liquidity)");

            Console.Write("   ");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("■ ");
            Console.ResetColor();
            Console.Write("Bid");

            Console.Write("   ");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("■ ");
            Console.ResetColor();
            Console.WriteLine("Ask");
        }

        private static string FormatPrice(decimal price)
            => price.ToString("000000.00", Invariant).PadLeft(PriceWidth);

        private static string FormatQty(decimal qty)
            => qty.ToString("0.000000", Invariant).PadLeft(QtyWidth);

        private static string Pad(string text, int width)
            => text.PadLeft(width);

        private static string Blank(int width)
            => new string(' ', width);

        private static string Center(string text, int width)
        {
            int padding = width - text.Length;
            int left = padding / 2 + text.Length;
            return text.PadLeft(left).PadRight(width);
        }
    }
}
