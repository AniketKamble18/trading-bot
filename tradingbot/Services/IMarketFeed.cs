using tradingbot.Models;

namespace tradingbot.Services;

public interface IMarketFeed
{
    IReadOnlyList<MarketQuote> GetQuotes();
    bool TryGetQuote(string symbol, out MarketQuote quote);
}
