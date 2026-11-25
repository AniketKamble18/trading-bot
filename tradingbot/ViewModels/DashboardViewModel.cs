using tradingbot.Models;

namespace tradingbot.ViewModels;

public sealed class DashboardViewModel
{
    public required IReadOnlyList<MarketQuote> Quotes { get; init; }
    public required PaperAccountSnapshot Account { get; init; }
    public string? Notice { get; init; }
    public bool IsError { get; init; }
}
