namespace tradingbot.Models;

public enum OrderSide
{
    Buy,
    Sell
}

public enum PaperOrderType
{
    Market
}

public sealed record MarketQuote(string Symbol, string Name, decimal Price, decimal Change, decimal ChangePercent, DateTimeOffset UpdatedAt);

public sealed record PaperOrder(Guid Id, string Symbol, OrderSide Side, int Quantity, decimal FillPrice, decimal Notional, DateTimeOffset SubmittedAt);

public sealed record PaperPosition(string Symbol, int Quantity, decimal AveragePrice, decimal LastPrice, decimal MarketValue, decimal UnrealizedPnl);

public sealed record PaperAccountSnapshot(
    decimal StartingBalance,
    decimal AvailableCash,
    decimal PortfolioValue,
    decimal RealizedPnl,
    decimal UnrealizedPnl,
    decimal DailyPnl,
    IReadOnlyList<PaperPosition> Positions,
    IReadOnlyList<PaperOrder> RecentOrders,
    int OrdersToday);

public sealed record PaperOrderRequest(string Symbol, OrderSide Side, int Quantity, PaperOrderType OrderType = PaperOrderType.Market);

public sealed record PaperOrderResult(bool Succeeded, string Message, PaperOrder? Order = null);
