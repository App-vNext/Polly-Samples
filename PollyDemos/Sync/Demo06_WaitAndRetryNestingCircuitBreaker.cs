using Polly;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Polly.CircuitBreaker;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    /// <summary>
    /// Demonstrates using the WaitAndRetry policy nesting CircuitBreaker.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// 
    /// Discussion:  What if the underlying system was completely down?  
    /// Keeping retrying would be pointless...
    /// ... and would leave the client hanging, retrying for successes which never come.
    /// 
    /// Enter circuit-breaker: 
    /// After too many failures, breaks the circuit for a period, during which it blocks calls + fails fast.
    /// - protects the downstream system from too many calls if it's really struggling (reduces load, so it can recover)
    /// - allows the client to get a fail response _fast, not wait for ages, if downstream is awol.
    /// 
    /// Obervations from this demo:
    /// Note how after the circuit decides to break, subsequent calls fail faster.
    /// Note how breaker gives underlying system time to recover ...
    /// ... by the time circuit closes again, underlying system has recovered!
    /// </summary>
    public class Demo06_WaitAndRetryNestingCircuitBreaker : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToCircuitBreaking;
        private int eventualFailuresForOtherReasons;

        public override string Description =>
            "This demonstrates CircuitBreaker.  When an underlying system is completely down or seriously struggling, it can be better to fail fast and not put calls through.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToCircuitBreaking = 0;
            eventualFailuresForOtherReasons = 0;

            progress.Report(ProgressWithMessage(nameof(Demo06_WaitAndRetryNestingCircuitBreaker)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            // Define our waitAndRetry policy: keep retrying with 200ms gaps.
            var waitAndRetryPolicy = Policy
                .Handle<Exception
                >(e =>
                    !(e is BrokenCircuitException)) // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForever(
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
                .CircuitBreaker(
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

            using (var client = new WebClient())
            {
                var internalCancel = false;
                totalRequests = 0;
                // Do the following until a key is pressed
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;
                    var watch = new Stopwatch();
                    watch.Start();

                    try
                    {
                        // Retry the following call according to the policy - 3 times.
                        waitAndRetryPolicy.Execute(
                            ct => // The Execute() overload takes a CancellationToken, but it happens the executed code does not honour it.
                            {
                                // This code is executed within the waitAndRetryPolicy 

                                var response = circuitBreakerPolicy.Execute<string>(
                                    () => // Note how we can also Execute() a Func<TResult> and pass back the value.
                                    {
                                        // This code is executed within the circuitBreakerPolicy 

                                        // Make a request and get a response
                                        return client.DownloadString(
                                            Configuration.WEB_API_ROOT + "/api/values/" + totalRequests);
                                    });

                                watch.Stop();

                                // Display the response message on the console
                                progress.Report(ProgressWithMessage("Response : " + response
                                                                                  + " (after " +
                                                                                  watch.ElapsedMilliseconds + "ms)",
                                    Color.Green));

                                eventualSuccesses++;
                            }
                            , cancellationToken // The cancellationToken passed in to Execute() enables the policy instance to cancel retries, when the token is signalled.
                        );
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
            new Statistic("Requests failed early by broken circuit", eventualFailuresDueToCircuitBreaking,
                Color.Magenta),
            new Statistic("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
        };
    }
}