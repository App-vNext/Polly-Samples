using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    /// <summary>
    /// Demonstrates the Retry strategy with delays between retry attempts.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations: We no longer have to guess how many retries are enough.
    /// All calls still succeed!  Yay!
    /// But we're still hammering that underlying server with retries.
    /// Imagine if lots of clients were doing that simultaneously
    ///  - could just increase load on an already-struggling server!
    /// </summary>
    public class Demo04_WaitAndRetryForever : SyncDemo
    {
        public override string Description =>
            "This demo also retries enough to always ensure success. But we haven't had to 'guess' how many retries were necessary. We just said: wait-and-retry-forever.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            // Let's call a web API service to make repeated requests to a server.
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailures = 0;
            totalRequests = 0;

            PrintHeader(progress, nameof(Demo04_WaitAndRetryForever));

            // Define our strategy:
            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = int.MaxValue, // Retry indefinitely
                Delay = TimeSpan.FromMilliseconds(200),
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    progress.Report(ProgressWithMessage($"Strategy logging: {exception.Message}", Color.Yellow));
                    retries++;
                    return default;
                }
            }).Build();

            var client = new HttpClient();
            var internalCancel = false;

            // Do the following until a key is pressed
            while (!(internalCancel || cancellationToken.IsCancellationRequested))
            {
                totalRequests++;

                try
                {
                    // Retry the following call according to the strategy.
                    // The cancellationToken passed in to Execute() enables the strategy to cancel retries, when the token is signalled.
                    strategy.Execute(token =>
                    {
                        // This code is executed within the strategy

                        var responseBody = IssueRequestAndProcessResponse(client, token);
                        progress.Report(ProgressWithMessage($"Response : {responseBody}", Color.Green));
                        eventualSuccesses++;

                    }, cancellationToken);
                }
                catch (Exception e)
                {
                    progress.Report(ProgressWithMessage($"Request {totalRequests} eventually failed with: {e.Message}", Color.Red));
                    eventualFailures++;
                }

                Thread.Sleep(500);
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
