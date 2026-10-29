using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// 0.0.0.0 so a test container can reach the app through host.docker.internal. Override with --urls.
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://0.0.0.0:5070");

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

foreach (var page in new[] { "orders", "catalog", "tasks", "checkout", "search" })
{
    app.MapGet($"/{page}", () => Results.File(Path.Combine(app.Environment.WebRootPath, $"{page}.html"), "text/html"));
}

var products = new[]
{
    new Product(1, "Claw hammer", 14.99m),
    new Product(2, "Hand saw", 22.50m),
    new Product(3, "Circular saw", 129.00m),
    new Product(4, "Combination pliers", 9.95m),
    new Product(5, "Screwdriver set", 18.00m),
    new Product(6, "Tape measure", 7.25m),
    new Product(7, "Spirit level", 16.40m),
    new Product(8, "Utility knife", 6.80m),
};

// "Sometimes slow": a backend that answers either quickly or after a cache miss.
static Task SometimesSlowAsync(int slowMs) => Task.Delay(Random.Shared.Next(2) == 0 ? 0 : slowMs);

app.MapGet("/api/orders", async () =>
{
    await Task.Delay(Random.Shared.Next(300, 1700));
    return Enumerable.Range(1, 5).Select(i => new { Number = 1000 + i, Total = 20m * i });
});

// Sorted by "popularity": usually the catalog order, but the ranking job reshuffles it now and then.
app.MapGet("/api/products", async (string? q) =>
{
    if (q is null)
    {
        await SometimesSlowAsync(1500);
        return Random.Shared.Next(2) == 0 ? products : products.OrderBy(_ => Random.Shared.Next()).ToArray();
    }

    await Task.Delay(400);
    return products.Where(p => p.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToArray();
});

// One task list for everybody, kept in memory until the app restarts.
var tasks = new ConcurrentDictionary<int, TaskItem>();
var nextTaskId = 0;

app.MapGet("/api/tasks", () => tasks.Values.OrderBy(t => t.Id));

app.MapPost("/api/tasks", (NewTask input) =>
{
    var task = new TaskItem(Interlocked.Increment(ref nextTaskId), input.Title.Trim(), Done: false);
    tasks[task.Id] = task;
    return Results.Created($"/api/tasks/{task.Id}", task);
});

app.MapPost("/api/tasks/{id:int}/complete", (int id) =>
    tasks.TryGetValue(id, out var task) ? Results.Ok(tasks[id] = task with { Done = true }) : Results.NotFound());

app.MapGet("/api/payment-methods", async () =>
{
    await SometimesSlowAsync(1200);
    return new[] { "Card", "Bank transfer", "Gift card" };
});

app.MapPost("/api/checkout", () => new { OrderNumber = Random.Shared.Next(10_000, 99_999) });

app.Run();

internal sealed record Product(int Id, string Name, decimal Price);

internal sealed record NewTask(string Title);

internal sealed record TaskItem(int Id, string Title, bool Done);
