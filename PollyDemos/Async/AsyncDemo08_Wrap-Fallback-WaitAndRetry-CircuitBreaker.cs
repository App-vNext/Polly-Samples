using Polly;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Wrap;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
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
    public class AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker : AsyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToCircuitBreaking;
        private int eventualFailuresForOtherReasons;

        public override string Description =>
            "This demo matches 06 and 07 (retry with circuit-breaker), but also introduces a Fallback: we can provide a graceful fallback message, on overall failure.";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToCircuitBreaking = 0;
            eventualFailuresForOtherReasons = 0;

            progress.Report(ProgressWithMessage(nameof(AsyncDemo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            Stopwatch watch = null;

            // Define our waitAndRetry policy: keep retrying with 200ms gaps.
            var waitAndRetryPolicy = Policy
                .Handle<Exception
                >(e =>
                    !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForeverAsync(
                    attempt => TimeSpan.FromMilliseconds(200),
                    (exception, calculatedWaitDuration) =>
                    {
                        progress.Report(ProgressWithMessage(".Log,then retry: " + exception.Message, Color.Yellow));
                        retries++;
                    });

            // Define our CircuitBreaker policy: Break if the action fails 4 times in a row.
            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(
                    4,
                    TimeSpan.FromSeconds(3),
                    (ex, breakDelay) =>
                    {
                        progress.Report(ProgressWithMessage(
                            ".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!",
                            Color.Magenta));
                        progress.Report(ProgressWithMessage("..due to: " + ex.Message, Color.Magenta));
                    },
                    () => progress.Report(ProgressWithMessage(".Breaker logging: Call ok! Closed the circuit again!",
                        Color.Magenta)),
                    () => progress.Report(ProgressWithMessage(".Breaker logging: Half-open: Next call is a trial!",
                        Color.Magenta))
                );

            // Define a fallback policy: provide a nice substitute message to the user, if we found the circuit was broken.
            var fallbackForCircuitBreaker = Policy<string>
                .Handle<BrokenCircuitException>()
                .FallbackAsync(
                    /* Demonstrates fallback value syntax */
                    "Please try again later [message substituted by fallback policy]",
                    async b =>
                    {
                        await Task.FromResult(true);
                        watch.Stop();
                        progress.Report(ProgressWithMessage("Fallback catches failed with: " + b.Exception.Message
                                                                                             + " (after " +
                                                                                             watch.ElapsedMilliseconds +
                                                                                             "ms)", Color.Red));
                        eventualFailuresDueToCircuitBreaking++;
                    }
                );

            // Define a fallback policy: provide a substitute string to the user, for any exception.
            var fallbackForAnyException = Policy<string>
                .Handle<Exception>()
                .FallbackAsync(
                    /* Demonstrates fallback action/func syntax */ async ct =>
                    {
                        await Task.FromResult(true);
                        /* do something else async if desired */
                        return "Please try again later [Fallback for any exception]";
                    },
                    async e =>
                    {
                        await Task.FromResult(true);
                        watch.Stop();
                        progress.Report(ProgressWithMessage(
                            "Fallback catches eventually failed with: " + e.Exception.Message
                                                                        + " (after " + watch.ElapsedMilliseconds +
                                                                        "ms)", Color.Red));
                        eventualFailuresForOtherReasons++;
                    }
                );

            // As demo07: we combine the waitAndRetryPolicy and circuitBreakerPolicy into a PolicyWrap, using the *static* Policy.Wrap syntax.
            var myResilienceStrategy = Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicy);

            // Added in demo08: we wrap the two fallback policies onto the front of the existing wrap too.  Demonstrates the *instance* wrap syntax. And the fact that the PolicyWrap myResilienceStrategy from above is just another Policy, which can be onward-wrapped too.  
            // With this pattern, you can build an overall resilience strategy programmatically, reusing some common parts (eg PolicyWrap myResilienceStrategy) but varying other parts (eg Fallback) individually for different calls.
            var policyWrap =
                fallbackForAnyException.WrapAsync(fallbackForCircuitBreaker.WrapAsync(myResilienceStrategy));
            // For info: Equivalent to: PolicyWrap<String> policyWrap = Policy.WrapAsync(fallbackForAnyException, fallbackForCircuitBreaker, waitAndRetryPolicy, circuitBreakerPolicy);

            totalRequests = 0;

            using (var client = new HttpClient())
            {
                var internalCancel = false;
                // Do the following until a key is pressed
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;
                    watch = new Stopwatch();
                    watch.Start();

                    try
                    {
                        // Manage the call according to the whole policy wrap
                        var response = await policyWrap.ExecuteAsync(ct =>
                                client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + totalRequests),
                            cancellationToken);

                        watch.Stop();

                        // Display the response message on the console
                        progress.Report(ProgressWithMessage(
                            "Response : " + response + " (after " + watch.ElapsedMilliseconds + "ms)", Color.Green));

                        eventualSuccesses++;
                    }
                    catch (Exception e
                    ) // try-catch not needed, now that we have a Fallback.Handle<Exception>.  It's only been left in to *demonstrate* it should never get hit.
                    {
                        throw new InvalidOperationException(
                            "Should never arrive here.  Use of fallbackForAnyException should have provided nice fallback value for any exceptions.",
                            e);
                    }

                    // Wait half second
                    await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);

                    internalCancel = TerminateDemosByKeyPress && Console.KeyAvailable;
                }
            }
        }

        public override Statistic[] LatestStatistics => new[]
        {
            new Statistic("Total requests made", totalRequests),
            new Statistic("Requests which eventually succeeded", eventualSuccesses, Color.Green),
            new Statistic("Retries made to help achieve success", retries, Color.Yellow),
            new Statistic("Requests failed early by broken circuit", eventualFailuresDueToCircuitBreaking,
                Color.Magenta),
            new Statistic("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
        };
    }
}