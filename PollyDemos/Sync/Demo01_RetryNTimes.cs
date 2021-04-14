using Polly;
using System;
using System.Net;
using System.Threading;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    /// <summary>
    /// Demonstrates the Retry policy coming into action.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// 
    /// Observations: There's no wait among these retries.  Can be appropriate sometimes.  
    /// In this case, no wait hasn't given underlying system time to recover, so calls still fail despite retries.
    /// </summary>
    public class Demo01_RetryNTimes : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailures;

        public override string Description =>
            "This demo demonstrates a first Retry.  It retries three times, immediately.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailures = 0;

            progress.Report(ProgressWithMessage(nameof(Demo01_RetryNTimes)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            // Let's call a web api service to make repeated requests to a server. 
            // The service is programmed to fail after 3 requests in 5 seconds.

            // Define our policy:
            var policy = Policy.Handle<Exception>().Retry(3, (exception, attempt) =>
            {
                // This is your new exception handler! 
                // Tell the user what they've won!
                progress.Report(ProgressWithMessage("Policy logging: " + exception.Message, Color.Yellow));
                retries++;
            });

            using (var client = new WebClient())
            {
                var internalCancel = false;
                totalRequests = 0;
                // Do the following until a key is pressed
                while (!internalCancel && !cancellationToken.IsCancellationRequested)
                {
                    totalRequests++;

                    try
                    {
                        // Retry the following call according to the policy - 3 times.
                        policy.Execute(
                            ct => // The Execute() overload takes a CancellationToken, but it happens the executed code does not honour it.
                            {
                                // This code is executed within the Policy 

                                // Make a request and get a response
                                var response =
                                    client.DownloadString(Configuration.WEB_API_ROOT + "/api/values/" + totalRequests);

                                // Display the response message on the console
                                progress.Report(ProgressWithMessage("Response : " + response, Color.Green));
                                eventualSuccesses++;
                            }
                            , cancellationToken // The cancellationToken passed in to Execute() enables the policy instance to cancel retries, when the token is signalled.
                        );
                    }
                    catch (Exception e)
                    {
                        progress.Report(ProgressWithMessage(
                            "Request " + totalRequests + " eventually failed with: " + e.Message, Color.Red));
                        eventualFailures++;
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
            new Statistic("Requests which eventually failed", eventualFailures, Color.Red),
        };
    }
}