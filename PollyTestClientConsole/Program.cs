using PollyDemos;
using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;
using PollyTestClientConsole;
using PollyTestClientConsole.Menu;
using Spectre.Console;
using Color = PollyDemos.OutputHelpers.Color;

Statistic[] statistics = [];
Progress<DemoProgress> progress = new();

progress.ProgressChanged += (_, args) =>
{
    foreach (var message in args.Messages)
    {
        WriteLineInColor(message.Message, message.Color);
    }

    statistics = args.Statistics;
};

// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
// Walk through the demos in order, to discover features.
// See <summary> at top of each demo class, for explanation.
// ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
List<ConsoleMenuItem> menu =
[
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
    new("12 - Hedging in latency mode",
        InvokeDemo<Demo12_LatencyHedging>),
    new("13 - Hedging in fallback mode: retry only",
        InvokeDemo<Demo13_FallbackHedging_RetryOnly>),
    new("14 - Hedging in fallback mode: retry with fallback",
        InvokeDemo<Demo14_FallbackHedging_RetryWithFallback>),
    new("15 - Hedging in parallel mode",
        InvokeDemo<Demo15_ParallelHedging>),
    new ("16 - Entity Framework with Retry N times",
        InvokeDemo<Demo16_EntityFramework_WithRetryNTimes>),

    new("-=Exit=-", () => Environment.Exit(0))
];

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
        WriteLineInColor($"{stat.Description.PadRight(longestDescription)}: {stat.Value}", stat.Color);
    }

    statistics = [];
}

void WriteLineInColor(string message, Color color)
{
    var consoleColor = Spectre.Console.Color.FromConsoleColor(color.ToConsoleColor());
    AnsiConsole.MarkupLineInterpolated($"[{consoleColor}]{message}[/]");
}
