namespace Miruken.Tests.Api
{
    using System;
    using Miruken.Callback;
    using Miruken.Concurrency;

    public class StockQuoteHandler : Handler
    {
        public static int Called;

        private readonly Random random = new Random();

        [Handles]
        public Promise<StockQuote> GetQuote(GetStockQuote quote)
        {
            ++Called;

            if (quote.Symbol == "EX")
                throw new Exception("Stock Exchange is down");

            return Promise.Resolved(new StockQuote
            {
                Symbol = quote.Symbol,
                Value = Convert.ToDecimal(random.NextDouble() * 10.0)
            });
        }

        [Handles]
        public Promise SellStock(SellStock sell)
        {
            ++Called;

            if (sell.Symbol == "EX")
                throw new Exception("Stock Exchange is down");

            return Promise.Empty;
        }
    }
}