namespace Miruken.Tests.Api;

using System;
using System.Threading;
using Miruken.Callback;
using Miruken.Concurrency;

public class StockQuoteHandler : Handler
{
    public static volatile int Called;

    private readonly Random random = new();

    [Handles]
    public Promise<StockQuote> GetQuote(GetStockQuote quote)
    {
        ++Called;

        if (quote.Symbol == "EX")
            throw new Exception("Stock Exchange is down.");

        return Promise.Resolved(new StockQuote
        {
            Symbol = quote.Symbol,
            Value = Convert.ToDecimal(random.NextDouble() * 10.0)
        });
    }

    [Handles]
    public Promise SellStock(SellStock sell)
    {
        Interlocked.Increment(ref Called);

        if (sell.Symbol == "EX")
            throw new Exception("Stock Exchange is down.");

        return Promise.Empty;
    }
}