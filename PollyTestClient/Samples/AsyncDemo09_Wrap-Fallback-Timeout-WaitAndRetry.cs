using Polly;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly.Fallback;
using Polly.Timeout;
using Polly.Wrap;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates using a Retry, a Timeout and two Fallback strategies.
    /// In this demo, the wait in the wait-and-retry is deliberately so long that the timeout strategy wrapping it will time it out
    /// (in lieu for now of a demo server endpoint responding slowly).
    ///
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations:
    /// - though the console logs that a retry will be made, the 4-second wait before the retry is pre-emptively timed-out by the two-second timeout
    /// - a fallback strategy then provides substitute message for the user
    /// - otherwise similar to demo08.
    /// </summary>
    public static class AsyncDemo09_Wrap_Fallback_Timeout_WaitAndRetry
    {
        public static async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine(typeof(AsyncDemo09_Wrap_Fallback_Timeout_WaitAndRetry).Name);
            Console.WriteLine("=======");

            var client = new HttpClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToTimeout = 0;
            int eventualFailuresForOtherReasons = 0;
            Stopwatch watch = null;

            // Define our timeout strategy: time out after 2 seconds.
            var timeoutPolicy = Policy
                .TimeoutAsync(TimeSpan.FromSeconds(2), TimeoutStrategy.Pessimistic);

            // Define our retry strategy: keep retrying with 4 second gaps. This is (intentionally) too long: to demonstrate that the timeout strategy will time out on this before waiting for the retry.
            var waitAndRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromSeconds(4),
                (exception, calculatedWaitDuration) =>
                {
                    ConsoleHelper.WriteLineInColor(".Log,then retry: " + exception.Message, ConsoleColor.Yellow);
                    retries++;
                });

            // Define a fallback strategy: provide a nice substitute message to the user, if we found the call was rejected due to the timeout.
            var fallbackForTimeout = Policy<String>
                .Handle<TimeoutRejectedException>()
                .FallbackAsync(
                    fallbackValue: "Please try again later [Fallback for timeout]",
                    onFallbackAsync: async b =>
                    {
                        watch.Stop();
                        ConsoleHelper.WriteInColor("Fallback catches failed with: " + b.Exception.Message, ConsoleColor.Red);
                        ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Red);
                        eventualFailuresDueToTimeout++;
                    }
                );

            // Define a fallback strategy: provide a substitute string to the user, for any exception.
            var fallbackForAnyException = Policy<String>
                .Handle<Exception>()
                .FallbackAsync(
                    fallbackAction:  async ct =>
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

            var policyWrap = fallbackForAnyException.WrapAsync(fallbackForTimeout).WrapAsync(timeoutPolicy).WrapAsync(waitAndRetryPolicy);

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
                catch (Exception e)
                {
                    throw new InvalidOperationException("Should never arrive here.  Use of fallbackForAnyException should have provided nice fallback value for any exceptions.", e);
                }

                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            }

            Console.WriteLine("");
            Console.WriteLine("Total requests made                     : " + i);
            Console.WriteLine("Requests which eventually succeeded     : " + eventualSuccesses);
            Console.WriteLine("Retries made to help achieve success    : " + retries);
            Console.WriteLine("Requests timed out by timeout strategy  : " + eventualFailuresDueToTimeout);
            Console.WriteLine("Requests which failed after longer delay: " + eventualFailuresForOtherReasons);

        }
    }
}
