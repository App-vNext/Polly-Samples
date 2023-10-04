using Polly;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Wrap;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates using a Retry, a CircuitBreaker and two Fallback strategies.
    /// Same as Demo07 but now uses Fallback strategies to provide substitute values, when the call still fails overall.
    ///
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations:
    /// - operation is identical to Demo06 and Demo07
    /// - except fallback strategies provide nice substitute messages, if still fails overall
    /// - onFallback delegate captures the stats that were captured in try/catches in demos 06 and 07
    /// - also demonstrates how you can use the same kind of strategy (Fallback in this case) twice (or more) in a pipeline.
    /// </summary>
    public static class AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker
    {
        public static async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine(typeof(AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker).Name);
            Console.WriteLine("=======");

            var client = new HttpClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToCircuitBreaking = 0;
            int eventualFailuresForOtherReasons = 0;
            Stopwatch watch = null;

            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException))
                .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(200),
                (exception, calculatedWaitDuration) =>
                {
                    ConsoleHelper.WriteLineInColor(".Log,then retry: " + exception.Message, ConsoleColor.Yellow);
                    retries++;
                });

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 4,
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    onBreak: (ex, breakDelay) =>
                    {
                        ConsoleHelper.WriteLineInColor(".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!", ConsoleColor.Magenta);
                        ConsoleHelper.WriteLineInColor("..due to: " + ex.Message, ConsoleColor.Magenta);
                    },
                    onReset: () => ConsoleHelper.WriteLineInColor(".Breaker logging: Call ok! Closed the circuit again!", ConsoleColor.Magenta),
                    onHalfOpen: () => ConsoleHelper.WriteLineInColor(".Breaker logging: Half-open: Next call is a trial!", ConsoleColor.Magenta)
                );

            // Define a fallback strategy: provide a nice substitute message to the user, if we found the circuit was broken.
            var fallbackForCircuitBreaker = Policy<String>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(
                    fallbackValue: "Please try again later [Fallback for broken circuit]",
                    onFallbackAsync: async b =>
                    {
                        watch.Stop();
                        ConsoleHelper.WriteInColor("Fallback catches failed with: " + b.Exception.Message, ConsoleColor.Red);
                        ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Red);
                        eventualFailuresDueToCircuitBreaking++;
                    }
                );

            // Define a fallback strategy: provide a substitute string to the user, for any exception.
            var fallbackForAnyException = Policy<String>
                .Handle<Exception>()
                .FallbackAsync(
                    fallbackAction: async ct =>
                    {
                        return "Please try again later [Fallback for any exception]";
                    },
                    onFallbackAsync: async e =>
                    {
                        watch.Stop();
                        ConsoleHelper.WriteInColor("Fallback catches eventually failed with: " + e.Exception.Message, ConsoleColor.Red);
                        ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Red);
                        eventualFailuresForOtherReasons++;
                    }
                );

            var myResilienceStrategy = Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicy);

            var policyWrap = fallbackForAnyException.WrapAsync(fallbackForCircuitBreaker.WrapAsync(myResilienceStrategy));

            int i = 0;
            while (!Console.KeyAvailable && !cancellationToken.IsCancellationRequested)
            {
                i++;
                watch = new Stopwatch();
                watch.Start();

                try
                {
                    string msg = await policyWrap.ExecuteAsync(ct => client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + i), cancellationToken);

                    watch.Stop();
                    ConsoleHelper.WriteInColor("Response : " + msg, ConsoleColor.Green);
                    ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Green);

                    eventualSuccesses++;
                }
                // This try-catch is not needed, since we have a Fallback for any Exceptions.
                // It's only been left in to *demonstrate* it should never get hit.
                catch (Exception e)
                    throw new InvalidOperationException("Should never arrive here.  Use of fallbackForAnyException should have provided nice fallback value for any exceptions.", e);
                }

                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            }

            Console.WriteLine("");
            Console.WriteLine("Total requests made                     : " + i);
            Console.WriteLine("Requests which eventually succeeded     : " + eventualSuccesses);
            Console.WriteLine("Retries made to help achieve success    : " + retries);
            Console.WriteLine("Requests failed early by broken circuit : " + eventualFailuresDueToCircuitBreaking);
            Console.WriteLine("Requests which failed after longer delay: " + eventualFailuresForOtherReasons);

        }
    }
}
