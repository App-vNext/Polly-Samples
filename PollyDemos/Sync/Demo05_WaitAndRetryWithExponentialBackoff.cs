using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    /// <summary>
    /// Demonstrates Retry strategy with calculated retry delays to back off.
    /// Loops through a series of HTTP requests, keeping track of each requested
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
    public class Demo05_WaitAndRetryWithExponentialBackoff : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailures;

        public override string Description =>
            "This demonstrates exponential back-off.  We have enough retries to ensure success.  But we don't hammer the server so hard: we increase the delay between each try.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            // Let's call a web API service to make repeated requests to a server.
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailures = 0;
            totalRequests = 0;

            progress.Report(ProgressWithMessage(nameof(Demo05_WaitAndRetryWithExponentialBackoff)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            // Define our strategy:
            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 6, // We could also retry indefinitely... but chose six times instead.
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; //The Exception property is nullable

                    // Tell the user what happened
                    progress.Report(ProgressWithMessage($"Strategy logging: {exception.Message}", Color.Yellow));
                    progress.Report(ProgressWithMessage($" ... automatically delaying for {args.RetryDelay.TotalMilliseconds}ms.", Color.Yellow));
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
                    strategy.Execute(ct =>
                    {
                        // This code is executed within the strategy

                        // Make a request and get a response
                        var url = $"{Configuration.WEB_API_ROOT}/api/values/{totalRequests}";
                        var response = client.Send(new HttpRequestMessage(HttpMethod.Get, url), ct);

                        // Display the response message on the console
                        using var stream = response.Content.ReadAsStream(ct);
                        using var streamReader = new StreamReader(stream);
                        progress.Report(ProgressWithMessage($"Response : {streamReader.ReadToEnd()}", Color.Green));
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
