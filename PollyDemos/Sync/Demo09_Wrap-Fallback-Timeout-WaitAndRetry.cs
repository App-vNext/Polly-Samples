using Polly;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
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
    public class Demo09_Wrap_Fallback_Timeout_WaitAndRetry : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToTimeout;
        private int eventualFailuresForOtherReasons;

        public override string Description =>
            "Demonstrates introducing a TimeoutPolicy. The TimeoutPolicy will eventually time-out on the retries that WaitAndRetry was orchestrating.  When we timeout, we again use a Fallback policy to substitute a more graceful message.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToTimeout = 0;
            eventualFailuresForOtherReasons = 0;

            progress.Report(ProgressWithMessage(nameof(Demo09_Wrap_Fallback_Timeout_WaitAndRetry)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            Stopwatch watch = null;

            // Define our timeout policy: time out after 2 seconds.  We will use the pessimistic timeout strategy, which forces a timeout - even when the underlying delegate doesn't support it.
            var timeoutPolicy = Policy
                .Timeout(TimeSpan.FromSeconds(2), TimeoutStrategy.Pessimistic,
                    // This use of onTimeout demonstrates the point about capturing walked-away-from Tasks with TimeoutStrategy.Pessimistic discussed in the Polly wiki, here: https://github.com/App-vNext/Polly/wiki/Timeout#pessimistic-timeout-1
                    (ctx, span, abandonedTask) =>
                    {
                        {
                            abandonedTask.ContinueWith(t =>
                            {
                                // ContinueWith important!: the abandoned task may very well still be executing, when the caller times out on waiting for it! 

                                if (t.IsFaulted)
                                    progress.Report(ProgressWithMessage(
                                        ".The task previously walked-away-from now terminated with exception: " +
                                        t.Exception.Message,
                                        Color.Yellow));
                                else if (t.IsCanceled)
                                    // (If the executed delegates do not honour cancellation, this IsCanceled branch may never be hit.  It can be good practice however to include, in case a Policy configured with TimeoutStrategy.Pessimistic is used to execute a delegate honouring cancellation.)  
                                    progress.Report(ProgressWithMessage(
                                        ".The task previously walked-away-from now was canceled.", Color.Yellow));
                                else
                                    // extra logic (if desired) for tasks which complete, despite the caller having 'walked away' earlier due to timeout.
                                    progress.Report(ProgressWithMessage(
                                        ".The task previously walked-away-from now eventually completed.",
                                        Color.Yellow));
                            });
                        }
                    }
                );

            // Define our waitAndRetry policy: keep retrying with 4 second gaps.  This is (intentionally) too long: to demonstrate that the timeout policy will time out on this before waiting for the retry.
            var waitAndRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryForever(
                    attempt => TimeSpan.FromSeconds(4),
                    (exception, calculatedWaitDuration) =>
                    {
                        progress.Report(ProgressWithMessage(".Log,then retry: " + exception.Message, Color.Yellow));
                        retries++;
                    });

            // Define a fallback policy: provide a nice substitute message to the user, if we found the call was rejected due to the timeout policy.
            var fallbackForTimeout = Policy<string>
                .Handle<TimeoutRejectedException>()
                .Fallback(
                    /* Demonstrates fallback value syntax */ "Please try again later [Fallback for timeout]",
                    b =>
                    {
                        watch.Stop();
                        progress.Report(ProgressWithMessage(
                            "Fallback catches failed with: " + b.Exception.Message + " (after " +
                            watch.ElapsedMilliseconds + "ms)", Color.Red));
                        eventualFailuresDueToTimeout++;
                    }
                );

            // Define a fallback policy: provide a substitute string to the user, for any exception.
            var fallbackForAnyException = Policy<string>
                .Handle<Exception>()
                .Fallback(
                    /* Demonstrates fallback action/func syntax */ () =>
                    {
                        return "Please try again later [Fallback for any exception]";
                    },
                    e =>
                    {
                        watch.Stop();

                        progress.Report(ProgressWithMessage(
                            "Fallback catches eventually failed with: " + e.Exception.Message + " (after " +
                            watch.ElapsedMilliseconds + "ms)", Color.Red));

                        eventualFailuresForOtherReasons++;
                    }
                );


            // Compared to previous demo08: here we use *instance* wrap syntax, to wrap all in one go.
            var policyWrap = fallbackForAnyException.Wrap(fallbackForTimeout).Wrap(timeoutPolicy)
                .Wrap(waitAndRetryPolicy);

            using (var client = new WebClient())
            {
                var internalCancel = false;
                totalRequests = 0;
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;
                    watch = new Stopwatch();
                    watch.Start();

                    try
                    {
                        // Manage the call according to the whole policy wrap.
                        var response =
                            policyWrap.Execute(
                                ct => client.DownloadString(Configuration.WEB_API_ROOT + "/api/values/" +
                                                            totalRequests), cancellationToken);

                        watch.Stop();

                        progress.Report(ProgressWithMessage(
                            "Response: " + response + "(after " + watch.ElapsedMilliseconds + "ms)", Color.Green));

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
            new Statistic("Requests timed out by timeout policy", eventualFailuresDueToTimeout, Color.Magenta),
            new Statistic("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
        };
    }
}