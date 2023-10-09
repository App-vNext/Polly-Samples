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

app.MapControllers();
app.UseRateLimiter();
app.Run("http://localhost:45179");

