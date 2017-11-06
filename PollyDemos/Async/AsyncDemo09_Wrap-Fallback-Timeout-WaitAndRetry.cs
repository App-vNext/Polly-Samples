﻿using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Fallback;
using Polly.Timeout;
using Polly.Wrap;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
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
    public class AsyncDemo09_Wrap_Fallback_Timeout_WaitAndRetry : AsyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToTimeout;
        private int eventualFailuresForOtherReasons;

        public override string Description => "Demonstrates introducing a TimeoutPolicy. The TimeoutPolicy will eventually time-out on the retries that WaitAndRetry was orchestrating.  When we timeout, we again use a Fallback policy to substitute a more graceful message.";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (cancellationToken == null) throw new ArgumentNullException(nameof(cancellationToken));
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToTimeout = 0;
            eventualFailuresForOtherReasons = 0;

            progress.Report(ProgressWithMessage(typeof(AsyncDemo09_Wrap_Fallback_Timeout_WaitAndRetry).Name));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(String.Empty));

            Stopwatch watch = null;

            // Define our timeout policy: time out after 2 seconds.  We will use the pessimistic timeout strategy, which forces a timeout - even when the underlying delegate doesn't support it.
            var timeoutPolicy = Policy
                .TimeoutAsync(TimeSpan.FromSeconds(2), TimeoutStrategy.Pessimistic);

            // Define our waitAndRetry policy: keep retrying with 4 second gaps.  This is (intentionally) too long: to demonstrate that the timeout policy will time out on this before waiting for the retry.
            var waitAndRetryPolicy = Policy
                .Handle<Exception>() // Exception filtering!  We don't retry if the inner circuit-breaker judges the underlying system is out of commission!
                .WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromSeconds(4),
                (exception, calculatedWaitDuration) =>
                {
                    progress.Report(ProgressWithMessage(".Log,then retry: " + exception.Message, Color.Yellow));
                    retries++;
                });

            // Define a fallback policy: provide a nice substitute message to the user, if we found the call was rejected due to the timeout policy.
            FallbackPolicy<String> fallbackForTimeout = Policy<String>
                .Handle<TimeoutRejectedException>()
                .FallbackAsync(
                    fallbackValue: /* Demonstrates fallback value syntax */ "Please try again later [Fallback for timeout]",
                    onFallbackAsync: async b =>
                    {
                        await Task.FromResult(true);
                        watch.Stop();
                        progress.Report(ProgressWithMessage("Fallback catches failed with: " + b.Exception.Message + " (after " + watch.ElapsedMilliseconds + "ms)", Color.Red));
                        eventualFailuresDueToTimeout++;
                    }
                );

            // Define a fallback policy: provide a substitute string to the user, for any exception.
            FallbackPolicy<String> fallbackForAnyException = Policy<String>
                .Handle<Exception>()
                .FallbackAsync(
                    fallbackAction: /* Demonstrates fallback action/func syntax */ async ct =>
                    {
                        await Task.FromResult(true); 
                        /* do something else async if desired */
                        return "Please try again later [Fallback for any exception]";
                    },
                    onFallbackAsync: async e =>
                    {
                        await Task.FromResult(true);
                        watch.Stop();
                        progress.Report(ProgressWithMessage("Fallback catches eventually failed with: " + e.Exception.Message + " (after " + watch.ElapsedMilliseconds + "ms)", Color.Red));
                        eventualFailuresForOtherReasons++;
                    }
                );

            // Compared to previous demo08: here we use *instance* wrap syntax, to wrap all in one go.
            PolicyWrap<String> policyWrap = fallbackForAnyException.WrapAsync(fallbackForTimeout).WrapAsync(timeoutPolicy).WrapAsync(waitAndRetryPolicy);

            using (var client = new HttpClient())
            {
                totalRequests = 0;
                bool internalCancel = false;

                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;
                    watch = new Stopwatch();
                    watch.Start();

                    try
                    {
                        // Manage the call according to the whole policy wrap
                        string response = await policyWrap.ExecuteAsync(ct =>
                                        client.GetStringAsync(Configuration.WEB_API_ROOT + "/api/values/" + totalRequests), cancellationToken);

                        watch.Stop();

                        progress.Report(ProgressWithMessage("Response: " + response + "(after " + watch.ElapsedMilliseconds + "ms)", Color.Green));

                        eventualSuccesses++;
                    }
                    catch (Exception e) // try-catch not needed, now that we have a Fallback.Handle<Exception>.  It's only been left in to *demonstrate* it should never get hit.
                    {
                        throw new InvalidOperationException("Should never arrive here.  Use of fallbackForAnyException should have provided nice fallback value for any exceptions.", e);
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
            new Statistic("Requests timed out by timeout policy", eventualFailuresDueToTimeout, Color.Magenta),
            new Statistic("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
        };

    }
}
