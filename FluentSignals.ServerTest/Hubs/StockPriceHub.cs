using FluentSignals.WebAssembly.Components.Pages.Demos.Models;
using Microsoft.AspNetCore.SignalR;


namespace FluentSignals.ServerTest.Hubs;

public class StockPriceHub : Hub
{
    private readonly ILogger<StockPriceHub> _logger;

    public StockPriceHub(ILogger<StockPriceHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        
        // Send initial stock prices when client connects
        var initialPrices = GetInitialStockPrices();
        foreach (var stock in initialPrices)
        {
            await Clients.Caller.SendAsync("PriceUpdate", stock);
            // Also send to stock-specific method
            await Clients.Caller.SendAsync($"PriceUpdate_{stock.Symbol}", stock);
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SubscribeToStock(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"stock-{symbol}");
        _logger.LogInformation("Client {ConnectionId} subscribed to {Symbol}", Context.ConnectionId, symbol);
        
        // Send current price for the subscribed stock
        var currentPrice = GetStockPrice(symbol);
        await Clients.Caller.SendAsync("PriceUpdate", currentPrice);
    }

    public async Task UnsubscribeFromStock(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"stock-{symbol}");
        _logger.LogInformation("Client {ConnectionId} unsubscribed from {Symbol}", Context.ConnectionId, symbol);
    }

    private List<StockPrice> GetInitialStockPrices()
    {
        return new List<StockPrice>
        {
            new StockPrice 
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
            new StockPrice 
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
            new StockPrice 
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
            }
        };
    }

    private StockPrice GetStockPrice(string symbol)
    {
        var prices = GetInitialStockPrices();
        return prices.FirstOrDefault(p => p.Symbol == symbol) ?? new StockPrice 
        { 
            Symbol = symbol, 
            Price = 100.00m,
            LastUpdate = DateTime.UtcNow 
        };
    }
}