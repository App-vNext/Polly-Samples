using PollyDemos.OutputHelpers;
using PollyDemos.Async;
using PollyDemos.Sync;

namespace PollyTestClientConsole
{
    internal class Program
    {
        private static async Task Main()
        {
            var statistics = Array.Empty<Statistic>();

            var progress = new Progress<DemoProgress>();
            progress.ProgressChanged += (_, args) =>
            {
                foreach (var message in args.Messages)
                {
                    WriteLineInColor(message.Message, message.Color.ToConsoleColor());
                }
                statistics = args.Statistics;
            };

            using var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Uncomment the samples you wish to run:
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Walk through the demos in order, to discover features.
            // See <summary> at top of each demo class, for explanation.
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Synchronous demos
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            //SyncDemo? syncDemo;
            //syncDemo = new Demo00_NoStrategy();
            //syncDemo = new Demo01_RetryNTimes();
            //syncDemo = new Demo02_WaitAndRetryNTimes();
            //syncDemo = new Demo03_WaitAndRetryNTimes_WithEnoughRetries();
            //syncDemo = new Demo04_WaitAndRetryForever();
            //syncDemo = new Demo05_WaitAndRetryWithExponentialBackoff();
            //syncDemo = new Demo06_WaitAndRetryNestingCircuitBreaker();
            //syncDemo = new Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline();
            //syncDemo = new Demo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker();
            //syncDemo = new Demo09_Pipeline_Fallback_Timeout_WaitAndRetry();
            //syncDemo!.Execute(cancellationToken, progress);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Asynchronous demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            AsyncDemo? asyncDemo;
            asyncDemo = new AsyncDemo00_NoStrategy();
            //asyncDemo = new AsyncDemo01_RetryNTimes();
            //asyncDemo = new AsyncDemo02_WaitAndRetryNTimes();
            //asyncDemo = new AsyncDemo03_WaitAndRetryNTimes_WithEnoughRetries();
            //asyncDemo = new AsyncDemo04_WaitAndRetryForever();
            //asyncDemo = new AsyncDemo05_WaitAndRetryWithExponentialBackoff();
            //asyncDemo = new AsyncDemo06_WaitAndRetryNestingCircuitBreaker();
            //asyncDemo = new AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPipeline();
            //asyncDemo = new AsyncDemo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker();
            //asyncDemo = new AsyncDemo09_Pipeline_Fallback_Timeout_WaitAndRetry();
            await asyncDemo!.ExecuteAsync(cancellationToken, progress);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Bulkhead demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            //new BulkheadAsyncDemo00_NoIsolation().ExecuteAsync(cancellationToken, progress).Wait();
            //new BulkheadAsyncDemo01_WithBulkheads().ExecuteAsync(cancellationToken, progress).Wait();

            Console.ReadKey();
            cancellationSource.Cancel();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            var longestDescription = statistics.Max(s => s.Description.Length);
            foreach (Statistic stat in statistics)
            {
                WriteLineInColor($"{stat.Description.PadRight(longestDescription)}: {stat.Value}",stat.Color.ToConsoleColor());
            }

            Console.ReadKey();
        }

        private static void WriteLineInColor(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
