using Polly;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly.CircuitBreaker;
using Polly.Wrap;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
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
    public class AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap : AsyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToCircuitBreaking;
        private int eventualFailuresForOtherReasons;

        public override string Description =>
            "This demonstrates CircuitBreaker (see Demo06), but uses the PolicyWrap syntax to compose the policies nicely. Only the underlying code differs.";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToCircuitBreaking = 0;
            eventualFailuresForOtherReasons = 0;

            progress.Report(
                ProgressWithMessage(nameof(AsyncDemo07_WaitAndRetryNestingCircuitBreakerUsingPolicyWrap)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            // Define our waitAndRetry policy: keep retrying with 200ms gaps.
            var waitAndRetryPolicy = Policy
                .Handle<Exception
                >(e =>
                    !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForeverAsync(
                    attempt => TimeSpan.FromMilliseconds(200),
                    (exception, calculatedWaitDuration) =>
                    {
                        // This is your new exception handler! 
                        // Tell the user what they've won!
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

            // New for demo07: combine the waitAndRetryPolicy and circuitBreakerPolicy into a PolicyWrap.
            var policyWrap = Policy.WrapAsync(waitAndRetryPolicy, circuitBreakerPolicy);

            using (var client = new HttpClient())
            {
                totalRequests = 0;
                var internalCancel = false;
                // Do the following until a key is pressed
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;
                    var watch = new Stopwatch();
                    watch.Start();

                    try
                    {
                        // Retry the following call according to the policy wrap
                        var response = await policyWrap.ExecuteAsync<string>(ct =>
                        {
                            // This code is executed through both policies in the wrap: WaitAndRetry outer, then CircuitBreaker inner.  Demo 06 shows a broken-out version of what this is equivalent to.

                            return client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + totalRequests);
                        }, cancellationToken);

                        watch.Stop();

                        // Display the response message on the console
                        progress.Report(ProgressWithMessage("Response : " + response
                                                                          + " (after " + watch.ElapsedMilliseconds +
                                                                          "ms)", Color.Green));

                        eventualSuccesses++;
                    }
                    catch (BrokenCircuitException b)
                    {
                        watch.Stop();

                        progress.Report(ProgressWithMessage("Request " + totalRequests + " failed with: " +
                                                            b.GetType().Name
                                                            + " (after " + watch.ElapsedMilliseconds + "ms)",
                            Color.Red));

                        eventualFailuresDueToCircuitBreaking++;
                    }
                    catch (Exception e)
                    {
                        watch.Stop();

                        progress.Report(ProgressWithMessage("Request " + totalRequests + " eventually failed with: " +
                                                            e.Message
                                                            + " (after " + watch.ElapsedMilliseconds + "ms)",
                            Color.Red));

                        eventualFailuresForOtherReasons++;
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