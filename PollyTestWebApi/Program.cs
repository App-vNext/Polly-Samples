using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRateLimiter(limiterOptions =>
{
    limiterOptions.AddFixedWindowLimiter("ThreeRequestsPerFiveSeconds", windowOptions =>
    {
        windowOptions.PermitLimit = 3;
        windowOptions.Window = TimeSpan.FromSeconds(5);
    });
    limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiterOptions.OnRejected = async (context, _) =>
    {
        await context.HttpContext.Response.WriteAsync("Too many requests have received. Request refused.", CancellationToken.None);
    };
});

var app = builder.Build();

// Register the ValuesController that is rate limited.
app.MapControllers();
app.UseRateLimiter();

// Register two endpoints that are not rate limited.
// They are used by the async demo 10 and 11
app.MapGet("/api/NonThrottledGood/{id}", ([FromRoute] int id) =>
{
    return $"Fast response from server to request #{id}";
});
app.MapGet("/api/NonThrottledFaulting/{id}", async ([FromRoute] int id) =>
{
    await Task.Delay(TimeSpan.FromSeconds(5));
    return $"Slow response from server to request #{id}";
});

app.Run("http://localhost:45179");
