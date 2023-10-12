using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync;

/// <summary>
/// Demonstrates the Retry strategy with delays between retry attempts.
/// Loops through a series of HTTP requests, keeping track of each requested
/// item and reporting server failures when encountering exceptions.
///
/// Observations: We now have waits among the retries.
/// In this case, still not enough wait - or not enough retries - for the underlying system to have recovered.
/// So we still fail some calls.
/// </summary>
public class Demo02_WaitAndRetryNTimes : SyncDemo
{
    public override string Description =>
        "Compared to previous demo, this demo adds waits between the retry attempts. Not always enough wait to ensure success, tho.";

    public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        EventualSuccesses = 0;
        Retries = 0;
        EventualFailures = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        var strategy = new ResiliencePipelineBuilder().AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200), // Wait between each try
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
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
                strategy.Execute(token =>
                {
                    var responseBody = IssueRequestAndProcessResponse(client, token);
                    progress.Report(ProgressWithMessage($"Response : {responseBody}", Color.Green));
                    EventualSuccesses++;

                }, cancellationToken);
            }
            catch (Exception e)
            {
                progress.Report(ProgressWithMessage($"Request {TotalRequests} eventually failed with: {e.Message}", Color.Red));
                EventualFailures++;
            }

            Thread.Sleep(500);
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
