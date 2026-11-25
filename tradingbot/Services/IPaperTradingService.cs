using tradingbot.Models;

namespace tradingbot.Services;

public interface IPaperTradingService
{
    PaperAccountSnapshot GetSnapshot(string accountId);
    PaperOrderResult PlaceOrder(string accountId, PaperOrderRequest request);
}
