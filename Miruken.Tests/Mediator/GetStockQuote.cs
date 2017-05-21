namespace Miruken.Tests.Mediator
{
    using System;
    using Miruken.Mediator;

    public class StockQuote
    {
        public string Symbol { get; set; }
        public double Value { get; set; }
    }

    public class GetStockQuote : IRequest<StockQuote>, IEquatable<GetStockQuote>
    {
        public GetStockQuote(string symbol)
        {
            Symbol = symbol;
        }

        public string Symbol { get; set; }

        public bool Equals(GetStockQuote other)
        {
            return Symbol == other?.Symbol;
        }
    }
}