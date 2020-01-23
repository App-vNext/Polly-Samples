using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PollyDemos.OutputHelpers;
using PollyDemos.Sync;
using PollyDemos.Async;

namespace PollyTestClientConsole
{
    internal class Program
    {
        private static readonly object lockObject = new object();

        private static void Main(string[] args)
        {
            Statistic[] statistics = new Statistic[0];

            var progress = new Progress<DemoProgress>();
            progress.ProgressChanged += (sender, progressArgs) =>
            {
                foreach (var message in progressArgs.Messages)
                    WriteLineInColor(message.Message, message.Color.ToConsoleColor());
                statistics = progressArgs.Statistics;
            };

            var cancellationSource = new CancellationTokenSource();
            var cancellationToken = cancellationSource.Token;

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Uncomment the samples you wish to run:
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Work through the demos in order, to discover features.
            // See <summary> at top of each demo class, for explanation.
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Synchronous demos
            //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            //new Demo00_NoPolicy().Execute(cancellationToken, progress);
            //new Demo01_RetryNTimes().Execute(cancellationToken, progress);
            //new Demo02_WaitAndRetryNTimes().Execute(cancellationToken, progress);
            //new Demo03_WaitAndRetryNTimes_WithEnoughRetries().Execute(cancellationToken, progress);
            //new Demo04_WaitAndRetryForever().Execute(cancellationToken, progress);
            //new Demo05_WaitAndRetryWithExponentialBackoff().Execute(cancellationToken, progress);
            //new Demo06_WaitAndRetryNestingCircuitBreaker().Execute(cancellationToken, progress);
            //new Demo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap().Execute(cancellationToken, progress);
            //new Demo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker().Execute(cancellationToken, progress);
            //new Demo09_Wrap_Fallback_Timeout_WaitAndRetry().Execute(cancellationToken, progress);

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Asynchronous demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            // These async demos use .Wait() (rather than await) with the async calls, only for the purposes of allowing the demos still to remain the primary execution thread and own the Console output.

            new AsyncDemo00_NoPolicy().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo01_RetryNTimes().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo02_WaitAndRetryNTimes().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo03_WaitAndRetryNTimes_WithEnoughRetries().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo04_WaitAndRetryForever().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo05_WaitAndRetryWithExponentialBackoff().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo06_WaitAndRetryNestingCircuitBreaker().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker().ExecuteAsync(cancellationToken, progress).Wait();
            //new AsyncDemo09_Wrap_Fallback_Timeout_WaitAndRetry().ExecuteAsync(cancellationToken, progress).Wait();

            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            // Bulkhead demos
            // ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

            //new BulkheadAsyncDemo00_NoIsolation().ExecuteAsync(cancellationToken, progress).Wait();
            //new BulkheadAsyncDemo01_WithBulkheads().ExecuteAsync(cancellationToken, progress).Wait();


            // Keep the console open.
            Console.ReadKey();
            cancellationSource.Cancel();
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            // Output statistics.
            int longestDescription = statistics.Max(s => s.Description.Length);
            foreach (Statistic stat in statistics)
                WriteLineInColor(stat.Description.PadRight(longestDescription) + ": " + stat.Value,
                    stat.Color.ToConsoleColor());

            // Keep the console open.
            Console.ReadKey();
        }

        public static void WriteLineInColor(string msg, ConsoleColor color)
        {
            lock (lockObject)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }
    }
}