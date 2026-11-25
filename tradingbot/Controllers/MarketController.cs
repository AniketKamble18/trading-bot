using Microsoft.AspNetCore.Mvc;
using tradingbot.Services;

namespace tradingbot.Controllers;

[ApiController]
[Route("api/market")]
public sealed class MarketController(IMarketFeed marketFeed) : ControllerBase
{
    [HttpGet("quotes")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult GetQuotes() => Ok(marketFeed.GetQuotes());
}
