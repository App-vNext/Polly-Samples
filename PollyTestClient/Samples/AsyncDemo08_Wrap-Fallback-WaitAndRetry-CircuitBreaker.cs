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
    /// Demonstrates a PolicyWrap including two Fallback policies (for different exceptions), WaitAndRetry and CircuitBreaker.
    /// As Demo07 - but now uses Fallback policies to provide substitute values, when the call still fails overall.
    ///  
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// 
    /// Obervations from this demo:
    /// - operation identical to Demo06 and Demo07  
    /// - except fallback policies provide nice substitute messages, if still fails overall
    /// - onFallback delegate captures the stats that were captured in try/catches in demos 06 and 07
    /// - also demonstrates how you can use the same kind of policy (Fallback in this case) twice (or more) in a wrap.
    /// </summary>
    public static class AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker
    {
        public static async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine(typeof(AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker).Name);
            Console.WriteLine("=======");
            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            var client = new HttpClient();
            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToCircuitBreaking = 0;
            int eventualFailuresForOtherReasons = 0;
            Stopwatch watch = null;

            // Define our waitAndRetry policy: keep retrying with 200ms gaps.
            var waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(200),
                (exception, calculatedWaitDuration) =>
                {
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

            // Define a fallback policy: provide a nice substitute message to the user, if we found the circuit was broken.
            FallbackPolicy<String> fallbackForCircuitBreaker = Policy<String>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(
                    fallbackValue: /* Demonstrates fallback value syntax */ "Please try again later [Fallback for broken circuit]",
                    onFallbackAsync: async b =>
                    {
                        watch.Stop();
                        ConsoleHelper.WriteInColor("Fallback catches failed with: " + b.Exception.Message, ConsoleColor.Red);
                        ConsoleHelper.WriteLineInColor(" (after " + watch.ElapsedMilliseconds + "ms)", ConsoleColor.Red);
                        eventualFailuresDueToCircuitBreaking++;
                    }
                );

            // Define a fallback policy: provide a substitute string to the user, for any exception.
            FallbackPolicy<String> fallbackForAnyException = Policy<String>
                .Handle<Exception>()
                .FallbackAsync(
                    fallbackAction: /* Demonstrates fallback action/func syntax */ async ct =>
                    {
                        /* do something else async if desired */
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

            // As demo07: we combine the waitAndRetryPolicy and circuitBreakerPolicy into a PolicyWrap, using the *static* Policy.Wrap syntax.
            PolicyWrap myResilienceStrategy = Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicy);

            // Added in demo08: we wrap the two fallback policies onto the front of the existing wrap too.  Demonstrates the *instance* wrap syntax. And the fact that the PolicyWrap myResilienceStrategy from above is just another Policy, which can be onward-wrapped too.  
            // With this pattern, you can build an overall resilience strategy programmatically, reusing some common parts (eg PolicyWrap myResilienceStrategy) but varying other parts (eg Fallback) individually for different calls.
            PolicyWrap<String> policyWrap = fallbackForAnyException.WrapAsync(fallbackForCircuitBreaker.WrapAsync(myResilienceStrategy));
            // For info: Equivalent to: PolicyWrap<String> policyWrap = Policy.Wrap(fallbackForAnyException, fallbackForCircuitBreaker, waitAndRetryPolicy, circuitBreakerPolicy);

            int i = 0;
            // Do the following until a key is pressed
            while (!Console.KeyAvailable && !cancellationToken.IsCancellationRequested)
            {
                i++;
                watch = new Stopwatch();
                watch.Start();

                try
                {
                    // Manage the call according to the whole policy wrap
                    string msg = await policyWrap.ExecuteAsync(ct => client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + i), cancellationToken);

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
