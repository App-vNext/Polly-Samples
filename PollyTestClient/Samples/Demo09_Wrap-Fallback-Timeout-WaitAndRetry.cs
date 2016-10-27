using Polly;
using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates a PolicyWrap including Fallback, Timeout and WaitAndRetry.
    /// In this demo, the wait in the wait-and-retry is deliberately so long that the timeout policy wrapping it will time it out
    /// (in lieu for now of a demo server endpoint responding slowly).
    /// 
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// 
    /// Obervations from this demo:
    /// - though the console logs that a retry will be made, the 4-second wait before the retry is pre-emptively timed-out by the two-second timeout
    /// - a fallback policy then provides substitute message for the user
    /// - otherwise similar to demo08.
    /// </summary>
    public static class Demo09_Wrap_Fallback_Timeout_WaitAndRetry
    {
        public static void Execute()
        {
            Console.WriteLine(MethodBase.GetCurrentMethod().DeclaringType.Name);
            Console.WriteLine("=======");
            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            var client = new WebClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToTimeout = 0;
            int eventualFailuresForOtherReasons = 0;
            Stopwatch watch = null;

            // Define our timeout policy: time out after 2 seconds.  We will use the pessimistic timeout strategy, which forces a timeout - even when the underlying delegate doesn't support it.
            var timeoutPolicy = Policy
                .Timeout(TimeSpan.FromSeconds(2), TimeoutStrategy.Pessimistic); 

            // Define our waitAndRetry policy: keep retrying with 4 second gaps.  This is (intentionally) too long: to demonstrate that the timeout policy will time out on this before waiting for the retry.
            RetryPolicy waitAndRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForever(
                attempt => TimeSpan.FromSeconds(4),
                (exception, calculatedWaitDuration) =>
                {
                    ConsoleHelper.WriteLineInColor(".Log,then retry: " + exception.Message, ConsoleColor.Yellow);
                    retries++;
                });

            // Define a fallback policy: provide a nice substitute message to the user, if we found the call was rejected due to the timeout policy.
            FallbackPolicy<String> fallbackForTimeout = Policy<String>
                .Handle<TimeoutRejectedException>()
                .Fallback(
                    fallbackValue: /* Demonstrates fallback value syntax */ "Please try again later [Fallback for timeout]",
                    onFallback: b =>
                    {
                        watch.Stop();
                        ConsoleHelper.WriteInColor("Fallback catches failed with: " + b.Exception.Message, ConsoleColor.Red);
                        ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Red);
                        eventualFailuresDueToTimeout++;
                    }
                );

            // Define a fallback policy: provide a substitute string to the user, for any exception.
            FallbackPolicy<String> fallbackForAnyException = Policy<String>
                .Handle<Exception>()
                .Fallback(
                    fallbackAction: /* Demonstrates fallback action/func syntax */ () => { return "Please try again later [Fallback for any exception]"; },
                    onFallback: e =>
                    {
                        watch.Stop();
                        ConsoleHelper.WriteInColor("Fallback catches eventually failed with: " + e.Exception.Message, ConsoleColor.Red);
                        ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Red);
                        eventualFailuresForOtherReasons++;
                    }
                );


            // Compared to previous demo08: here we use *instance* wrap syntax, to wrap all in one go.
            PolicyWrap<String> policyWrap = fallbackForAnyException.Wrap(fallbackForTimeout).Wrap(timeoutPolicy).Wrap(waitAndRetryPolicy);

            int i = 0;
            while (!Console.KeyAvailable)
            {
                i++;
                watch = new Stopwatch();
                watch.Start();

                try
                {
                    // Manage the call according to the whole policy wrap.
                    string msg = policyWrap.Execute(() => client.DownloadString(Configuration.WEB_API_ROOT + "/api/values/" + i));

                    watch.Stop();

                    // Display the response message on the console
                    ConsoleHelper.WriteInColor("Response : " + msg, ConsoleColor.Green);
                    ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Green);

                    eventualSuccesses++;
                }
                catch (Exception e) // try-catch not needed, now that we have a Fallback.Handle<Exception>.  It's only been left in to *demonstrate* it should never get hit.
                {
                    throw new InvalidOperationException("Should never arrive here.  Use of fallbackForAnyException should have provided nice fallback value for any exceptions.", e);
                }

                // Wait half second
                Thread.Sleep(500);
            }

            Console.WriteLine("");
            Console.WriteLine("Total requests made                     : " + i);
            Console.WriteLine("Requests which eventually succeeded     : " + eventualSuccesses);
            Console.WriteLine("Retries made to help achieve success    : " + retries);
            Console.WriteLine("Requests timed out by timeout policy    : " + eventualFailuresDueToTimeout);
            Console.WriteLine("Requests which failed after longer delay: " + eventualFailuresForOtherReasons);

        }
    }
}
