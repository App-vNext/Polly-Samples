using PollyDemos;
using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

using PollyTestClientConsole;
using PollyTestClientConsole.Menu;

Statistic[] statistics = Array.Empty<Statistic>();
Progress<DemoProgress> progress = new();
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
    new("00 - No strategy",
        InvokeDemo<Demo00_NoStrategy>),
    new("01 - Retry N times",
        InvokeDemo<Demo01_RetryNTimes>),
    new("02 - Wait and retry N times",
        InvokeDemo<Demo02_WaitAndRetryNTimes>),
    new("03 - Wait and retry N times, N big enough to guarantee success",
        InvokeDemo<Demo03_WaitAndRetryNTimes_WithEnoughRetries>),
    new("04 - Wait and retry forever",
        InvokeDemo<Demo04_WaitAndRetryForever>),
    new("05 - Wait and retry with exponential back-off",
        InvokeDemo<Demo05_WaitAndRetryWithExponentialBackoff>),
    new("06 - Wait and retry nesting circuit breaker",
        InvokeDemo<Demo06_WaitAndRetryNestingCircuitBreaker>),
    new("07 - Wait and retry chaining with circuit breaker by using Pipeline",
        InvokeDemo<Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline>),
    new("08 - Fallback, Retry, and CircuitBreaker in a Pipeline",
        InvokeDemo<Demo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker>),
    new("09 - Fallback, Timeout, and Retry in a Pipeline",
        InvokeDemo<Demo09_Pipeline_Fallback_Timeout_WaitAndRetry>),
    new("10 - Without isolation: Faulting calls swamp resources, also prevent good calls",
        InvokeDemo<Demo10_SharedConcurrencyLimiter>),
    new("11 - With isolation: Faulting calls separated, do not swamp resources, good calls still succeed",
        InvokeDemo<Demo11_MultipleConcurrencyLimiters>),

    new("-=Exit=-", () => Environment.Exit(0))
};

ConsoleMenu.PrintSplashScreen();
ConsoleMenu.Run(menu);


void InvokeDemo<T>() where T : DemoBase, new()
{
    using var cancellationSource = new CancellationTokenSource();
    var cancellationToken = cancellationSource.Token;

    new T().ExecuteAsync(cancellationToken, progress).Wait();
    cancellationSource.Cancel();

    PrintStatisticsThenClear();
}

void PrintStatisticsThenClear()
{
    var longestDescription = statistics.Max(s => s.Description.Length);

    Console.WriteLine();
    Console.WriteLine(new string('=', longestDescription));
    Console.WriteLine();

    foreach (Statistic stat in statistics)
    {
        WriteLineInColor($"{stat.Description.PadRight(longestDescription)}: {stat.Value}", stat.Color.ToConsoleColor());
    }

    statistics = Array.Empty<Statistic>();
}

void WriteLineInColor(string message, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.WriteLine(message);
    Console.ResetColor();
}
