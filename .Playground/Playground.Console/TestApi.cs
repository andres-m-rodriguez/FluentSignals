using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Playground.Console.Models;

namespace Playground.Console;

public static class TestApi
{
    public static async Task StartAsync(string localHostPath)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development,
            ApplicationName = typeof(TestApi).Assembly.FullName
        });
        
        // Suppress the default logging
        builder.Logging.ClearProviders();
        
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Configure Kestrel to use a specific port
        builder.WebHost.UseUrls(localHostPath);

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // ===== TEST ENDPOINTS =====

        // Basic GET endpoint
        app.MapGet(
            "/api/test",
            () => new { message = "Hello from test API!", timestamp = DateTime.UtcNow }
        );

        // Users CRUD endpoints
        var users = new List<User>
        {
            new()
            {
                Id = 1,
                Name = "John Doe",
                Email = "john@example.com",
                Age = 30,
            },
            new()
            {
                Id = 2,
                Name = "Jane Smith",
                Email = "jane@example.com",
                Age = 25,
            },
        };

        app.MapGet("/api/users", async () =>
        {
            return users;
        });

        app.MapGet(
            "/api/users/{id}",
            (int id) =>
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                return user is not null ? Results.Ok(user) : Results.NotFound();
            }
        );

        app.MapPost(
            "/api/users",
            (User user) =>
            {
                user.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
                users.Add(user);
                return Results.Created($"/api/users/{user.Id}", user);
            }
        );

        app.MapPut(
            "/api/users/{id}",
            (int id, User updatedUser) =>
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                if (user is null)
                    return Results.NotFound();

                user.Name = updatedUser.Name;
                user.Email = updatedUser.Email;
                user.Age = updatedUser.Age;
                return Results.Ok(user);
            }
        );

        app.MapDelete(
            "/api/users/{id}",
            (int id) =>
            {
                var user = users.FirstOrDefault(u => u.Id == id);
                if (user is null)
                    return Results.NotFound();

                users.Remove(user);
                return Results.NoContent();
            }
        );

        // Search endpoint with query parameters
        app.MapGet(
            "/api/users/search",
            (string? name, int? minAge) =>
            {
                var query = users.AsQueryable();

                if (!string.IsNullOrEmpty(name))
                    query = query.Where(u =>
                        u.Name.Contains(name, StringComparison.OrdinalIgnoreCase)
                    );

                if (minAge.HasValue)
                    query = query.Where(u => u.Age >= minAge.Value);

                return query.ToList();
            }
        );

        // Simulated error endpoints
        app.MapGet("/api/error/500", () => Results.StatusCode(500));
        app.MapGet(
            "/api/error/400",
            () => Results.BadRequest(new { error = "Bad request example" })
        );
        app.MapGet("/api/error/401", () => Results.Unauthorized());

        // Delayed response endpoint (for testing timeouts/retries)
        app.MapGet(
            "/api/slow",
            async () =>
            {
                await Task.Delay(3000);
                return new { message = "This was slow!" };
            }
        );

        // Random failure endpoint (for testing retry logic)
        var random = new Random();
        app.MapGet(
            "/api/unstable",
            () =>
            {
                if (random.Next(3) == 0) // 33% chance of success
                    return Results.Ok(new { message = "Lucky you!" });
                else
                    return Results.StatusCode(503); // Service Unavailable
            }
        );

        await app.RunAsync();
    }
}
