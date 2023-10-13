using PollyDemos.OutputHelpers;
using PollyDemos.Async;
using PollyDemos.Sync;
using PollyTestClientConsole.Menu;

namespace PollyTestClientConsole;

internal static class Program
{
    static Statistic[] statistics = Array.Empty<Statistic>();
    static readonly Progress<DemoProgress> progress = new();
    private static void Main()
    {
        progress.ProgressChanged += (_, args) =>
        {
            foreach (var message in args.Messages)
            {
                WriteLineInColor(message.Message, message.Color.ToConsoleColor());
            }
            statistics = args.Statistics;
        };

        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        // Walk through the demos in order, to discover features.
        // See <summary> at top of each demo class, for explanation.
        // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
        List<ConsoleMenuItem> menu = new()
        {
            new("Sync: No strategy",
                InvokeSyncDemo<Demo00_NoStrategy>),
            new("Sync: Retry N times",
                InvokeSyncDemo<Demo01_RetryNTimes>),
            new("Sync: Wait and retry N times",
                InvokeSyncDemo<Demo02_WaitAndRetryNTimes>),
            new("Sync: Wait and retry N times, N big enough to guarantee success",
                InvokeSyncDemo<Demo03_WaitAndRetryNTimes_WithEnoughRetries>),
            new("Sync: Wait and retry forever",
                InvokeSyncDemo<Demo04_WaitAndRetryForever>),
            new("Sync: Wait and retry with exponential back-off",
                InvokeSyncDemo<Demo05_WaitAndRetryWithExponentialBackoff>),
            new("Sync: Wait and retry nesting circuit breaker",
                InvokeSyncDemo<Demo06_WaitAndRetryNestingCircuitBreaker>),
            new("Sync: Wait and retry chaining with circuit breaker by using Pipeline",
                InvokeSyncDemo<Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline>),
            new("Sync: Fallback, Retry, and CircuitBreaker in a Pipeline",
                InvokeSyncDemo<Demo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker>),
            new("Sync: Fallback, Timeout, and Retry in a Pipeline",
                InvokeSyncDemo<Demo09_Pipeline_Fallback_Timeout_WaitAndRetry>),

            new("Async: No strategy",
                InvokeAsyncDemo<AsyncDemo00_NoStrategy>),
            new("Async: Retry N times",
                InvokeAsyncDemo<AsyncDemo01_RetryNTimes>),
            new("Async: Wait and retry N times",
                InvokeAsyncDemo<AsyncDemo02_WaitAndRetryNTimes>),
            new("Async: Wait and retry N times, N big enough to guarantee success",
                InvokeAsyncDemo<AsyncDemo03_WaitAndRetryNTimes_WithEnoughRetries>),
            new("Async: Wait and retry forever",
                InvokeAsyncDemo<AsyncDemo04_WaitAndRetryForever>),
            new("Async: Wait and retry with exponential back-off",
                InvokeAsyncDemo<AsyncDemo05_WaitAndRetryWithExponentialBackoff>),
            new("Async: Wait and retry nesting circuit breaker",
                InvokeAsyncDemo<AsyncDemo06_WaitAndRetryNestingCircuitBreaker>),
            new("Async: Wait and retry chaining with circuit breaker by using Pipeline",
                InvokeAsyncDemo<AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPipeline>),
            new("Async: Fallback, Retry, and CircuitBreaker in a Pipeline",
                InvokeAsyncDemo<AsyncDemo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker>),
            new("Async: Fallback, Timeout, and Retry in a Pipeline",
                InvokeAsyncDemo<AsyncDemo09_Pipeline_Fallback_Timeout_WaitAndRetry>),
            new("Async: Without isolation: Faulting calls swamp resources, also prevent good calls",
                InvokeAsyncDemo<AsyncDemo10_SharedConcurrencyLimiter>),
            new("Async: With isolation: Faulting calls separated, do not swamp resources, good calls still succeed",
                InvokeAsyncDemo<AsyncDemo11_MultipleConcurrencyLimiters>),

            new("-=Exit=-", () => Environment.Exit(0)),
        };

        ConsoleMenu.PrintSplashScreen();
        ConsoleMenu.Run(menu);
    }

    static void InvokeSyncDemo<T>() where T : SyncDemo, new()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;

        new T().Execute(cancellationToken, progress);
        cancellationSource.Cancel();

        ShowStatistics();
        statistics = Array.Empty<Statistic>();
    }

    static void InvokeAsyncDemo<T>() where T : AsyncDemo, new()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;

        new T().ExecuteAsync(cancellationToken, progress).Wait();
        cancellationSource.Cancel();

        ShowStatistics();
        statistics = Array.Empty<Statistic>();
    }

    private static void ShowStatistics()
    {
        var longestDescription = statistics.Max(s => s.Description.Length);

        Console.WriteLine();
        Console.WriteLine(new string('=', longestDescription));
        Console.WriteLine();

        foreach (Statistic stat in statistics)
        {
            WriteLineInColor($"{stat.Description.PadRight(longestDescription)}: {stat.Value}", stat.Color.ToConsoleColor());
        }
    }

    private static void WriteLineInColor(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
