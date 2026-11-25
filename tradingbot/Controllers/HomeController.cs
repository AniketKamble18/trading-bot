using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using tradingbot.Models;
using tradingbot.Services;
using tradingbot.ViewModels;

namespace tradingbot.Controllers
{
    public class HomeController(IMarketFeed marketFeed, IPaperTradingService paperTradingService) : Controller
    {
        public IActionResult Index()
        {
            var notice = TempData["OrderNotice"] as string;
            var error = TempData["OrderError"] as string;
            return View(new DashboardViewModel
            {
                Quotes = marketFeed.GetQuotes(),
                Account = paperTradingService.GetSnapshot(HttpContext.Session.Id),
                Notice = error ?? notice,
                IsError = error is not null
            });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
