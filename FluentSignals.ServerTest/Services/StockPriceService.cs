using Microsoft.AspNetCore.SignalR;
using FluentSignals.ServerTest.Hubs;
using FluentSignals.WebAssembly.Components.Pages.Demos.Models;

namespace FluentSignals.ServerTest.Services;

public class StockPriceService : BackgroundService
{
    private readonly IHubContext<StockPriceHub> _hubContext;
    private readonly ILogger<StockPriceService> _logger;
    private readonly Dictionary<string, StockPrice> _currentPrices;
    private readonly Random _random = new();

    public StockPriceService(IHubContext<StockPriceHub> hubContext, ILogger<StockPriceService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        
        // Initialize stock prices
        _currentPrices = new Dictionary<string, StockPrice>
        {
            ["AAPL"] = new StockPrice 
            { 
                Symbol = "AAPL", 
                Price = 150.25m, 
                Change = 2.50m, 
                ChangePercent = 1.69m,
                Volume = 52_000_000,
                High = 151.50m,
                Low = 148.75m,
                Open = 149.00m,
                LastUpdate = DateTime.UtcNow 
            },
            ["GOOGL"] = new StockPrice 
            { 
                Symbol = "GOOGL", 
                Price = 2750.80m, 
                Change = -15.20m, 
                ChangePercent = -0.55m,
                Volume = 28_000_000,
                High = 2780.00m,
                Low = 2745.00m,
                Open = 2765.00m,
                LastUpdate = DateTime.UtcNow 
            },
            ["MSFT"] = new StockPrice 
            { 
                Symbol = "MSFT", 
                Price = 380.45m, 
                Change = 5.15m, 
                ChangePercent = 1.37m,
                Volume = 35_000_000,
                High = 382.00m,
                Low = 375.50m,
                Open = 376.00m,
                LastUpdate = DateTime.UtcNow 
            },
            ["AMZN"] = new StockPrice 
            { 
                Symbol = "AMZN", 
                Price = 3245.12m, 
                Change = 12.34m, 
                ChangePercent = 0.38m,
                Volume = 41_000_000,
                High = 3250.00m,
                Low = 3230.00m,
                Open = 3235.00m,
                LastUpdate = DateTime.UtcNow 
            },
            ["TSLA"] = new StockPrice 
            { 
                Symbol = "TSLA", 
                Price = 875.65m, 
                Change = -23.45m, 
                ChangePercent = -2.61m,
                Volume = 65_000_000,
                High = 900.00m,
                Low = 870.00m,
                Open = 895.00m,
                LastUpdate = DateTime.UtcNow 
            }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Price Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Update 1-3 random stocks
                var stocksToUpdate = _random.Next(1, 4);
                var symbols = _currentPrices.Keys.ToList();
                
                for (int i = 0; i < stocksToUpdate; i++)
                {
                    var symbol = symbols[_random.Next(symbols.Count)];
                    var updatedPrice = UpdateStockPrice(_currentPrices[symbol]);
                    
                    // Send update to all connected clients
                    await _hubContext.Clients.All.SendAsync("PriceUpdate", updatedPrice, stoppingToken);
                    
                    // Also send to stock-specific method
                    await _hubContext.Clients.All.SendAsync($"PriceUpdate_{symbol}", updatedPrice, stoppingToken);
                    
                    // Also send to stock-specific groups
                    await _hubContext.Clients.Group($"stock-{symbol}").SendAsync("PriceUpdate", updatedPrice, stoppingToken);
                    
                    _logger.LogDebug("Updated price for {Symbol}: ${Price}", symbol, updatedPrice.Price);
                }

                // Wait 1-3 seconds before next update
                var delay = TimeSpan.FromSeconds(_random.Next(1, 4));
                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock prices");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Stock Price Service stopped");
    }

    private StockPrice UpdateStockPrice(StockPrice current)
    {
        // Generate random price movement (between -2% and +2%)
        var changePercent = (_random.NextDouble() - 0.5) * 4;
        var priceChange = current.Price * (decimal)(changePercent / 100);
        
        var newPrice = current.Price + priceChange;
        
        // Update high/low if necessary
        var newHigh = Math.Max(current.High, newPrice);
        var newLow = Math.Min(current.Low, newPrice);
        
        // Add volume
        var volumeChange = _random.Next(-1_000_000, 2_000_000);
        var newVolume = Math.Max(0, current.Volume + volumeChange);
        
        // Calculate change from open
        var changeFromOpen = newPrice - current.Open;
        var changePercentFromOpen = (changeFromOpen / current.Open) * 100;
        
        return new StockPrice
        {
            Symbol = current.Symbol,
            Price = Math.Round(newPrice, 2),
            Change = Math.Round(changeFromOpen, 2),
            ChangePercent = Math.Round(changePercentFromOpen, 2),
            Volume = newVolume,
            High = Math.Round(newHigh, 2),
            Low = Math.Round(newLow, 2),
            Open = current.Open,
            LastUpdate = DateTime.UtcNow,
            MarketStatus = IsMarketOpen() ? "Open" : "Closed"
        };
    }

    private bool IsMarketOpen()
    {
        var now = DateTime.UtcNow;
        var easternTime = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
        
        // Market is open Monday-Friday, 9:30 AM - 4:00 PM ET
        if (easternTime.DayOfWeek == DayOfWeek.Saturday || easternTime.DayOfWeek == DayOfWeek.Sunday)
            return false;
            
        var marketOpen = new TimeSpan(9, 30, 0);
        var marketClose = new TimeSpan(16, 0, 0);
        
        return easternTime.TimeOfDay >= marketOpen && easternTime.TimeOfDay < marketClose;
    }
}