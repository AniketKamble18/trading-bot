using Microsoft.Extensions.Caching.Memory;
using tradingbot.Models;

namespace tradingbot.Services;

public sealed class PaperTradingService(IMarketFeed marketFeed, IMemoryCache memoryCache) : IPaperTradingService
{
    private const decimal StartingBalance = 1_000_000m;
    private const decimal MaxOrderValue = 200_000m;
    private const decimal MaxDailyLoss = 20_000m;
    private readonly object _gate = new();

    public PaperAccountSnapshot GetSnapshot(string accountId)
    {
        lock (_gate)
        {
            return CreateSnapshot(GetAccount(accountId));
        }
    }

    public PaperOrderResult PlaceOrder(string accountId, PaperOrderRequest request)
    {
        lock (_gate)
        {
            var account = GetAccount(accountId);
            if (!Enum.IsDefined(request.Side) || !Enum.IsDefined(request.OrderType))
            {
                return new PaperOrderResult(false, "Choose a supported order side and type.");
            }

            if (request.OrderType != PaperOrderType.Market)
            {
                return new PaperOrderResult(false, "Only market orders are enabled in this paper-trading phase.");
            }

            if (request.Quantity is < 1 or > 100_000)
            {
                return new PaperOrderResult(false, "Quantity must be between 1 and 100,000.");
            }

            if (string.IsNullOrWhiteSpace(request.Symbol))
            {
                return new PaperOrderResult(false, "Choose an instrument from the demo market feed.");
            }

            if (!marketFeed.TryGetQuote(request.Symbol, out var quote))
            {
                return new PaperOrderResult(false, "That instrument is not available in the demo market feed.");
            }

            var notional = quote.Price * request.Quantity;
            if (request.Side == OrderSide.Buy)
            {
                if (notional > MaxOrderValue)
                {
                    return new PaperOrderResult(false, $"Order value exceeds the demo entry limit of ₹{MaxOrderValue:N0}.");
                }

                if (CreateSnapshot(account).DailyPnl <= -MaxDailyLoss)
                {
                    return new PaperOrderResult(false, $"New entries are paused at the ₹{MaxDailyLoss:N0} demo loss limit; existing positions can still be reduced.");
                }

                if (notional > account.AvailableCash)
                {
                    return new PaperOrderResult(false, "Insufficient virtual cash for this order.");
                }

                account.AvailableCash -= notional;
                if (account.Positions.TryGetValue(quote.Symbol, out var existing))
                {
                    existing.AveragePrice = ((existing.AveragePrice * existing.Quantity) + notional) / (existing.Quantity + request.Quantity);
                    existing.Quantity += request.Quantity;
                }
                else
                {
                    account.Positions.Add(quote.Symbol, new MutablePosition(request.Quantity, quote.Price));
                }
            }
            else
            {
                if (!account.Positions.TryGetValue(quote.Symbol, out var position) || position.Quantity < request.Quantity)
                {
                    return new PaperOrderResult(false, "Sell quantity cannot exceed your open paper position. Short selling is disabled.");
                }

                account.RealizedPnl += (quote.Price - position.AveragePrice) * request.Quantity;
                account.AvailableCash += notional;
                position.Quantity -= request.Quantity;
                if (position.Quantity == 0)
                {
                    account.Positions.Remove(quote.Symbol);
                }
            }

            var order = new PaperOrder(Guid.NewGuid(), quote.Symbol, request.Side, request.Quantity, quote.Price, notional, DateTimeOffset.UtcNow);
            account.Orders.Insert(0, order);
            return new PaperOrderResult(true, $"{request.Side} {request.Quantity:N0} {quote.Symbol} filled at ₹{quote.Price:N2} (simulated).", order);
        }
    }

    private AccountState GetAccount(string accountId)
    {
        return memoryCache.GetOrCreate($"paper-account:{accountId}", entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromMinutes(30);
            return new AccountState();
        })!;
    }

    private PaperAccountSnapshot CreateSnapshot(AccountState account)
    {
        var positions = account.Positions.Select(entry =>
        {
            marketFeed.TryGetQuote(entry.Key, out var quote);
            var marketValue = quote.Price * entry.Value.Quantity;
            return new PaperPosition(entry.Key, entry.Value.Quantity, entry.Value.AveragePrice, quote.Price,
                marketValue, (quote.Price - entry.Value.AveragePrice) * entry.Value.Quantity);
        }).ToArray();
        var unrealizedPnl = positions.Sum(position => position.UnrealizedPnl);

        return new PaperAccountSnapshot(
            StartingBalance,
            account.AvailableCash,
            account.AvailableCash + positions.Sum(position => position.MarketValue),
            account.RealizedPnl,
            unrealizedPnl,
            account.RealizedPnl + unrealizedPnl,
            positions,
            account.Orders.Take(10).ToArray(),
            account.Orders.Count);
    }

    private sealed class AccountState
    {
        public Dictionary<string, MutablePosition> Positions { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<PaperOrder> Orders { get; } = [];
        public decimal AvailableCash { get; set; } = StartingBalance;
        public decimal RealizedPnl { get; set; }
    }

    private sealed class MutablePosition(int quantity, decimal averagePrice)
    {
        public int Quantity { get; set; } = quantity;
        public decimal AveragePrice { get; set; } = averagePrice;
    }
}
