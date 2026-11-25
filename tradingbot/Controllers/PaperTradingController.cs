using Microsoft.AspNetCore.Mvc;
using tradingbot.Models;
using tradingbot.Services;

namespace tradingbot.Controllers;

public sealed class PaperTradingController(IPaperTradingService paperTradingService) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceOrder(string symbol, OrderSide side, int quantity)
    {
        if (!ModelState.IsValid)
        {
            TempData["OrderError"] = "Enter a valid instrument, side, and quantity.";
            return RedirectToAction("Index", "Home");
        }

        var result = paperTradingService.PlaceOrder(HttpContext.Session.Id, new PaperOrderRequest(symbol, side, quantity));
        TempData[result.Succeeded ? "OrderNotice" : "OrderError"] = result.Message;
        return RedirectToAction("Index", "Home");
    }
}
