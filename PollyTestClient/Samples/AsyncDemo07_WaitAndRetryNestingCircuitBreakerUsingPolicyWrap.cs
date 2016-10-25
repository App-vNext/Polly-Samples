using Polly;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly.CircuitBreaker;
using Polly.Wrap;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates using the WaitAndRetry policy nesting CircuitBreaker.
    /// Same as Demo06 - but this time demonstrates combining the policies, using PolicyWrap.
    /// 
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// 
    /// Obervations from this demo:
    /// The operation is identical to Demo06.  
    /// The code demonstrates how using the PolicyWrap makes your combined-Policy-strategy more concise, at the point of execution.
    /// </summary>
    public static class AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap
    {
        public static async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine(typeof(AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap).Name);
            Console.WriteLine("=======");
            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            var client = new HttpClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToCircuitBreaking = 0;
            int eventualFailuresForOtherReasons = 0;

            // Define our waitAndRetry policy: keep retrying with 200ms gaps.
            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(200),
                (exception, calculatedWaitDuration) =>
                {
                    // This is your new exception handler! 
                    // Tell the user what they've won!
                    ConsoleHelper.WriteLineInColor(".Log,then retry: " + exception.Message, ConsoleColor.Yellow);
                    retries++;
                });

            // Define our CircuitBreaker policy: Break if the action fails 4 times in a row.
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

            // New for demo07: combine the waitAndRetryPolicy and circuitBreakerPolicy into a PolicyWrap.
            PolicyWrap policyWrap = Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicy);

            int i = 0;
            // Do the following until a key is pressed
            while (!Console.KeyAvailable && !cancellationToken.IsCancellationRequested)
            {
                i++;
                Stopwatch watch = new Stopwatch();
                watch.Start();

                try
                {
                    // Retry the following call according to the policy wrap
                    string msg = await policyWrap.ExecuteAsync<String>(ct =>
                    {
                        // This code is executed through both policies in the wrap: WaitAndRetry outer, then CircuitBreaker inner.  Demo 06 shows a broken-out version of what this is equivalent to.

                        return client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + i);
                    }, cancellationToken);

                    watch.Stop();

                    // Display the response message on the console
                    ConsoleHelper.WriteInColor("Response : " + msg, ConsoleColor.Green);
                    ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Green);

                    eventualSuccesses++;
                }
                catch (BrokenCircuitException b)
                {
                    watch.Stop();
                    ConsoleHelper.WriteInColor("Request " + i + " failed with: " + b.GetType().Name, ConsoleColor.Red);
                    ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Red);
                    eventualFailuresDueToCircuitBreaking++;
                }
                catch (Exception e)
                {
                    watch.Stop();
                    ConsoleHelper.WriteInColor("Request " + i + " eventually failed with: " + e.Message, ConsoleColor.Red);
                    ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Red);
                    eventualFailuresForOtherReasons++;
                }

                // Wait half second
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
