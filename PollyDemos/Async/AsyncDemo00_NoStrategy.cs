using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
    /// <summary>
    /// Uses no strategy.  Demonstrates behavior of 'faulting server' we are testing against.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// </summary>
    public class AsyncDemo00_NoStrategy : AsyncDemo
    {
        public override string Description =>
            "This demo demonstrates how our faulting server behaves, with no Polly strategy in use.";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            // Let's call a web API service to make repeated requests to a server.
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            eventualFailures = 0;
            totalRequests = 0;

            PrintHeader(progress, nameof(AsyncDemo00_NoStrategy));

            var client = new HttpClient();
            var internalCancel = false;

            // Do the following until a key is pressed
            while (!(internalCancel || cancellationToken.IsCancellationRequested))
            {
                totalRequests++;

                try
                {
                    // Make a request and get a response
                    var responseBody = await IssueRequestAndProcessResponseAsync(client, cancellationToken);

                    // Display the response message on the console
                    progress.Report(ProgressWithMessage($"Response : {responseBody}", Color.Green));
                    eventualSuccesses++;
                }
                catch (Exception e)
                {
                    progress.Report(ProgressWithMessage($"Request {totalRequests} eventually failed with: {e.Message}", Color.Red));
                    eventualFailures++;
                }

                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
                internalCancel = TerminateDemosByKeyPress && Console.KeyAvailable;
            }
        }

        public override Statistic[] LatestStatistics => new Statistic[]
        {
            new("Total requests made", totalRequests),
            new("Requests which eventually succeeded", eventualSuccesses, Color.Green),
            new("Retries made to help achieve success", retries, Color.Yellow),
            new("Requests which eventually failed", eventualFailures, Color.Red),
        };
    }
}
