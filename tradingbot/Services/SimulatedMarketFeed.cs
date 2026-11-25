using Microsoft.AspNetCore.SignalR;
using tradingbot.Hubs;
using tradingbot.Models;

namespace tradingbot.Services;

public sealed class SimulatedMarketFeed(IHubContext<MarketHub> hubContext, ILogger<SimulatedMarketFeed> logger) : BackgroundService, IMarketFeed
{
    private readonly object _gate = new();
    private readonly Dictionary<string, InstrumentState> _instruments = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NIFTY"] = new("NIFTY", "NIFTY 50", 24_350m),
        ["BANKNIFTY"] = new("BANKNIFTY", "BANK NIFTY", 52_100m),
        ["SENSEX"] = new("SENSEX", "SENSEX", 80_250m),
        ["FINNIFTY"] = new("FINNIFTY", "FIN NIFTY", 23_800m),
        ["RELIANCE"] = new("RELIANCE", "RELIANCE", 1_420m),
        ["INFY"] = new("INFY", "INFOSYS", 1_860m)
    };

    public IReadOnlyList<MarketQuote> GetQuotes()
    {
        lock (_gate)
        {
            return _instruments.Values.Select(ToQuote).ToArray();
        }
    }

    public bool TryGetQuote(string symbol, out MarketQuote quote)
    {
        lock (_gate)
        {
            if (_instruments.TryGetValue(symbol, out var instrument))
            {
                quote = ToQuote(instrument);
                return true;
            }
        }

        quote = null!;
        return false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                IReadOnlyList<MarketQuote> quotes;
                lock (_gate)
                {
                    foreach (var instrument in _instruments.Values)
                    {
                        var movement = (decimal)(Random.Shared.NextDouble() - 0.5) * instrument.ReferencePrice * 0.0008m;
                        instrument.Price = Math.Max(0.05m, instrument.Price + movement);
                        instrument.UpdatedAt = DateTimeOffset.UtcNow;
                    }

                    quotes = _instruments.Values.Select(ToQuote).ToArray();
                }

                await hubContext.Clients.All.SendAsync("MarketUpdate", quotes, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "The simulated market feed stopped unexpectedly.");
            throw;
        }
    }

    private static MarketQuote ToQuote(InstrumentState instrument)
    {
        var change = instrument.Price - instrument.ReferencePrice;
        return new MarketQuote(instrument.Symbol, instrument.Name, instrument.Price, change,
            change / instrument.ReferencePrice * 100m, instrument.UpdatedAt);
    }

    private sealed class InstrumentState(string symbol, string name, decimal referencePrice)
    {
        public string Symbol { get; } = symbol;
        public string Name { get; } = name;
        public decimal ReferencePrice { get; } = referencePrice;
        public decimal Price { get; set; } = referencePrice;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
