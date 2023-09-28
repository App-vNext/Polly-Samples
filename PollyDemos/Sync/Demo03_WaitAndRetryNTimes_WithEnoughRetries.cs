using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    /// <summary>
    /// Demonstrates the Retry strategy with delays between retry attempts.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations: We now have waits and enough retries: all calls now succeed!  Yay!
    /// But we kind-a had to guess how many retries would be enough before the server responded again ...
    /// (and we're hammering that server with retries)
    /// </summary>
    public class Demo03_WaitAndRetryNTimes_WithEnoughRetries : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailures;

        public override string Description =>
            "Compared to previous demo, this adds enough waiting and retrying to always ensure success.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            // Let's call a web api service to make repeated requests to a server.
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailures = 0;
            totalRequests = 0;

            progress.Report(ProgressWithMessage(nameof(Demo03_WaitAndRetryNTimes_WithEnoughRetries)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            // Let's call a web api service to make repeated requests to a server.
            // The service is programmed to fail after 3 requests in 5 seconds.

            // Define our strategy:
            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 20, // Retry up to 20 times! - should be enough that we eventually succeed.
                Delay = TimeSpan.FromMilliseconds(200),  // Wait 200ms between each try
                OnRetry = args =>
                {
                    // Due to fact how we have defined the ShouldHandle this delegate is called only if an exception occurred.
                    // Please note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; //The Exception property is nullable

                    // Tell the user what happened
                    progress.Report(ProgressWithMessage("Strategy logging: " + exception.Message, Color.Yellow));
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
                    // Retry the following call according to the strategy - 20 times.
                    // the Execute() overload takes a CancellationToken, but the decorated code does not honour it.
                    strategy.Execute(
                        ct =>
                        {
                            // This code is executed within the strategy

                            // Make a request and get a response
                            var url = $"{Configuration.WEB_API_ROOT}/api/values/{totalRequests}";
                            // Please note that the cancellation token is not used here.
                            var response = client.Send(new HttpRequestMessage(HttpMethod.Get, url));

                            // Display the response message on the console
                            // Please note that the cancellation token is not used here.
                            using var stream = response.Content.ReadAsStream();
                            using var streamReader = new StreamReader(stream);
                            progress.Report(ProgressWithMessage($"Response : {streamReader.ReadToEnd()}", Color.Green));
                            eventualSuccesses++;
                        }, cancellationToken); // The cancellationToken passed in to Execute() enables the strategy to cancel retries, when the token is signalled.
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
