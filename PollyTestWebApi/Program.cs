using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace PollyTestWebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
        }
    }
}
