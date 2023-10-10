using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
    /// <summary>
    /// Demonstrates the Retry strategy coming into action.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations: There's no wait among these retries. It can be appropriate sometimes.
    /// In this case, no wait hasn't given underlying system time to recover, so calls still fail despite retries.
    /// </summary>
    public class AsyncDemo01_RetryNTimes : AsyncDemo
    {
        public override string Description =>
            "This demo demonstrates a first Retry.  It retries three times, immediately.";

        public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            EventualSuccesses = 0;
            Retries = 0;
            EventualFailures = 0;
            TotalRequests = 0;

            PrintHeader(progress);

            // Define our strategy:
            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 3, // Retry up to 3 times
                OnRetry = args =>
                {
                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; // The Exception property is nullable

                    // Tell the user what happened
                    progress.Report(ProgressWithMessage($"Strategy logging: {exception.Message}", Color.Yellow));
                    Retries++;
                    return default;
                }
            }).Build();

            var client = new HttpClient();
            var internalCancel = false;

            while (!(internalCancel || cancellationToken.IsCancellationRequested))
            {
                TotalRequests++;

                try
                {
                    // Retry the following call according to the strategy.
                    // The cancellationToken passed in to ExecuteAsync() enables the strategy to cancel retries when the token is signalled.
                    await strategy.ExecuteAsync(async token =>
                    {
                        // This code is executed within the strategy

                        var responseBody = await IssueRequestAndProcessResponseAsync(client, token);
                        progress.Report(ProgressWithMessage($"Response : {responseBody}", Color.Green));
                        EventualSuccesses++;

                    }, cancellationToken);
                }
                catch (Exception e)
                {
                    progress.Report(ProgressWithMessage($"Request {TotalRequests} eventually failed with: {e.Message}", Color.Red));
                    EventualFailures++;
                }

                await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
                internalCancel = ShouldTerminateByKeyPress();
            }
        }

        public override Statistic[] LatestStatistics => new Statistic[]
        {
            new("Total requests made", TotalRequests),
            new("Requests which eventually succeeded", EventualSuccesses, Color.Green),
            new("Retries made to help achieve success", Retries, Color.Yellow),
            new("Requests which eventually failed", EventualFailures, Color.Red),
        };
    }
}
