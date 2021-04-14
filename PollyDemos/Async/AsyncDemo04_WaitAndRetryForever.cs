using Polly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
    /// <summary>
    /// Demonstrates the WaitAndRetryForever policy.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// 
    /// Observations: We no longer have to guess how many retries are enough.  
    /// All calls still succeed!  Yay!
    /// But we're still hammering that underlying server with retries. 
    /// Imagine if lots of clients were doing that simultaneously
    ///  - could just increase load on an already-struggling server!
    /// </summary>
    public class AsyncDemo04_WaitAndRetryForever : AsyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailures;

        public override string Description =>
            "This demo also retries enough to always ensure success.  But we haven't had to 'guess' how many retries were necessary.  We just said: wait-and-retry-forever.";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailures = 0;

            progress.Report(ProgressWithMessage(nameof(AsyncDemo04_WaitAndRetryForever)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            // Define our policy:
            var policy = Policy.Handle<Exception>().WaitAndRetryForeverAsync(
                attempt => TimeSpan.FromMilliseconds(200), // Wait 200ms between each try.
                (exception, calculatedWaitDuration) => // Capture some info for logging!
                {
                    // This is your new exception handler! 
                    // Tell the user what they've won!
                    progress.Report(ProgressWithMessage("Log, then retry: " + exception.Message, Color.Yellow));
                    retries++;
                });

            using (var client = new HttpClient())
            {
                totalRequests = 0;
                var internalCancel = false;
                // Do the following until a key is pressed
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;

                    try
                    {
                        // Retry the following call according to the policy - 15 times.
                        await policy.ExecuteAsync(async token =>
                        {
                            // This code is executed within the Policy 

                            // Make a request and get a response
                            var msg = await client.GetStringAsync(
                                Configuration.WEB_API_ROOT + "/api/values/" + totalRequests);

                            // Display the response message on the console
                            progress.Report(ProgressWithMessage("Response : " + msg, Color.Green));
                            eventualSuccesses++;
                        }, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        progress.Report(ProgressWithMessage(
                            "Request " + totalRequests + " eventually failed with: " + e.Message, Color.Red));
                        eventualFailures++;
                    }

                    // Wait half second before the next request.
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
            new Statistic("Requests which eventually failed", eventualFailures, Color.Red),
        };
    }
}