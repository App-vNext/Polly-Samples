using Polly;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Wrap;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
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
    public class Demo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToCircuitBreaking;
        private int eventualFailuresForOtherReasons;

        public override string Description => "This demo matches 06 and 07 (retry with circuit-breaker), but also introduces a Fallback: we can provide a graceful fallback message, on overall failure.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToCircuitBreaking = 0;
            eventualFailuresForOtherReasons = 0;

            progress.Report(ProgressWithMessage(typeof(Demo08_Wrap_Fallback_WaitAndRetry_CircuitBreaker).Name));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(String.Empty));

            Stopwatch watch = null;

            // Define our waitAndRetry policy: keep retrying with 200ms gaps.
            RetryPolicy waitAndRetryPolicy = Policy
                .Handle<Exception>(e => !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForever(
                attempt => TimeSpan.FromMilliseconds(200),
                (exception, calculatedWaitDuration) =>
                {
                    progress.Report(ProgressWithMessage(".Log,then retry: " + exception.Message, Color.Yellow));
                    retries++;
                });

            // Define our CircuitBreaker policy: Break if the action fails 4 times in a row.
            CircuitBreakerPolicy circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreaker(
                    exceptionsAllowedBeforeBreaking: 4,
                    durationOfBreak: TimeSpan.FromSeconds(3),
                    onBreak: (ex, breakDelay) =>
                    {
                        progress.Report(ProgressWithMessage(".Breaker logging: Breaking the circuit for " + breakDelay.TotalMilliseconds + "ms!", Color.Magenta));
                        progress.Report(ProgressWithMessage("..due to: " + ex.Message, Color.Magenta));
                    },
                    onReset: () => progress.Report(ProgressWithMessage(".Breaker logging: Call ok! Closed the circuit again!", Color.Magenta)),
                    onHalfOpen: () => progress.Report(ProgressWithMessage(".Breaker logging: Half-open: Next call is a trial!", Color.Magenta))
                );


            // Define a fallback policy: provide a nice substitute message to the user, if we found the circuit was broken.
            FallbackPolicy<String> fallbackForCircuitBreaker = Policy<String>
                .Handle<BrokenCircuitException>()
                .Fallback(
                    fallbackValue: /* Demonstrates fallback value syntax */ "Please try again later [message substituted by fallback policy]",  
                    onFallback: b =>
                    {
                        watch.Stop();

                        progress.Report(ProgressWithMessage("Fallback catches failed with: " + b.Exception.Message
                            + " (after " + watch.ElapsedMilliseconds + "ms)", Color.Red));

                        eventualFailuresDueToCircuitBreaking++;
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

                        progress.Report(ProgressWithMessage("Fallback catches eventually failed with: " + e.Exception.Message
                            + " (after " + watch.ElapsedMilliseconds + "ms)", Color.Red));

                        eventualFailuresForOtherReasons++;
                    }
                );


            // As demo07: we combine the waitAndRetryPolicy and circuitBreakerPolicy into a PolicyWrap, using the *static* Policy.Wrap syntax.
            PolicyWrap myResilienceStrategy = Policy.Wrap(waitAndRetryPolicy, circuitBreakerPolicy);

            // Added in demo08: we wrap the two fallback policies onto the front of the existing wrap too.  Demonstrates the *instance* wrap syntax. And the fact that the PolicyWrap myResilienceStrategy from above is just another Policy, which can be onward-wrapped too.  
            // With this pattern, you can build an overall resilience strategy programmatically, reusing some common parts (eg PolicyWrap myResilienceStrategy) but varying other parts (eg Fallback) individually for different calls.
            PolicyWrap<String> policyWrap = fallbackForAnyException.Wrap(fallbackForCircuitBreaker.Wrap(myResilienceStrategy));
            // For info: Equivalent to: PolicyWrap<String> policyWrap = Policy.Wrap(fallbackForAnyException, fallbackForCircuitBreaker, waitAndRetryPolicy, circuitBreakerPolicy);

            using (var client = new WebClient())
            {
                bool internalCancel = false;
                totalRequests = 0;
                // Do the following until a key is pressed
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;
                    watch = new Stopwatch();
                    watch.Start();

                    try
                    {
                        // Manage the call according to the whole policy wrap.
                        string response =
                            policyWrap.Execute(ct => client.DownloadString(Configuration.WEB_API_ROOT + "/api/values/" + totalRequests), cancellationToken);

                        watch.Stop();

                        // Display the response message on the console
                        progress.Report(ProgressWithMessage("Response : " + response + " (after " + watch.ElapsedMilliseconds + "ms)", Color.Green));

                        eventualSuccesses++;
                    }
                    catch (Exception e) // try-catch not needed, now that we have a Fallback.Handle<Exception>.  It's only been left in to *demonstrate* it should never get hit.
                    {
                        throw new InvalidOperationException("Should never arrive here.  Use of fallbackForAnyException should have provided nice fallback value for any exceptions.", e);
                    }

                    // Wait half second
                    Thread.Sleep(500);

                    internalCancel = TerminateDemosByKeyPress && Console.KeyAvailable;
                }
            }

        }

        public override Statistic[] LatestStatistics => new[]
        {
            new Statistic("Total requests made", totalRequests),
            new Statistic("Requests which eventually succeeded", eventualSuccesses, Color.Green),
            new Statistic("Retries made to help achieve success", retries, Color.Yellow),
            new Statistic("Requests failed early by broken circuit", eventualFailuresDueToCircuitBreaking, Color.Magenta),
            new Statistic("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
        };

    }
}
