using tradingbot.Hubs;
using tradingbot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddSignalR();
builder.Services.AddSingleton<SimulatedMarketFeed>();
builder.Services.AddSingleton<IMarketFeed>(services => services.GetRequiredService<SimulatedMarketFeed>());
builder.Services.AddHostedService(services => services.GetRequiredService<SimulatedMarketFeed>());
builder.Services.AddSingleton<IPaperTradingService, PaperTradingService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapStaticAssets();
app.MapHub<MarketHub>("/hubs/market");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
