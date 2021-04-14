using Polly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
    /// <summary>
    /// Demonstrates WaitAndRetry policy with calculated retry delays to back off.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// 
    /// Observations: All calls still succeed!  Yay!
    /// But we didn't hammer the underlying server so hard - we backed off.
    /// That's healthier for it, if it might be struggling ...
    /// ... and if a lot of clients might be doing this simultaneously.
    /// 
    /// ... What if the underlying system was totally down tho?  
    /// ... Keeping trying forever would be counterproductive (so, see Demo06)
    /// </summary>
    public class AsyncDemo05_WaitAndRetryWithExponentialBackoff : AsyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailures;

        public override string Description =>
            "This demonstrates exponential backoff.  We have enough retries to ensure success.  But we don't hammer the server so hard: we increase the delay between each try.";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailures = 0;

            progress.Report(ProgressWithMessage(nameof(AsyncDemo05_WaitAndRetryWithExponentialBackoff)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            var policy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(
                    6, // We can also do this with WaitAndRetryForever... but chose WaitAndRetry this time.
                    attempt => TimeSpan.FromSeconds(0.1 * Math.Pow(2,
                                                        attempt)), // Back off!  2, 4, 8, 16 etc times 1/4-second
                    (exception, calculatedWaitDuration) => // Capture some info for logging!
                    {
                        // This is your new exception handler! 
                        // Tell the user what they've won!
                        progress.Report(ProgressWithMessage("Exception: " + exception.Message, Color.Yellow));
                        progress.Report(ProgressWithMessage(
                            " ... automatically delaying for " + calculatedWaitDuration.TotalMilliseconds + "ms.",
                            Color.Yellow));
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