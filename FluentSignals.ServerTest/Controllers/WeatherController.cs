using Microsoft.AspNetCore.Mvc;

namespace FluentSignals.ServerTest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherController> _logger;

    public WeatherController(ILogger<WeatherController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int? delay = null)
    {
        // Simulate network delay if requested
        if (delay.HasValue)
        {
            await Task.Delay(delay.Value);
        }

        var forecast = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();

        return Ok(forecast);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        await Task.Delay(500); // Simulate network delay

        if (id < 1 || id > 10)
        {
            return NotFound(new { message = "Weather forecast not found" });
        }

        return Ok(new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(id)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WeatherForecast forecast)
    {
        await Task.Delay(1000); // Simulate processing

        if (forecast.TemperatureC < -273)
        {
            return BadRequest(new { message = "Temperature cannot be below absolute zero" });
        }

        forecast.Date = DateOnly.FromDateTime(DateTime.Now);
        return CreatedAtAction(nameof(GetById), new { id = Random.Shared.Next(1, 10) }, forecast);
    }

    [HttpGet("error/{code}")]
    public IActionResult SimulateError(int code)
    {
        return code switch
        {
            400 => BadRequest(new { message = "Bad request simulation" }),
            401 => Unauthorized(new { message = "Unauthorized simulation" }),
            403 => StatusCode(403, new { message = "Forbidden simulation" }),
            404 => NotFound(new { message = "Not found simulation" }),
            500 => StatusCode(500, new { message = "Internal server error simulation" }),
            503 => StatusCode(503, new { message = "Service unavailable simulation" }),
            _ => Ok(new { message = "Success" })
        };
    }
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string? Summary { get; set; }
}