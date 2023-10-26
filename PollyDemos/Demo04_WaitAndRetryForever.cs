using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Demonstrates the Retry strategy with delays between retry attempts. <br/>
///     Loops through a series of HTTP requests, keeping track of each requested <br/>
///     item and reporting server failures when encountering exceptions.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>We no longer have to guess how many retries are enough.</item>
///         <item>All calls still succeed! Yay!</item>
///         <item>But we're still hammering that underlying server with retries.</item>
///         <item>Imagine if lots of clients were doing that simultaneously <br/>
///         ... could just increase load on an already-struggling server!</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: ... request #N(...)": Response received on time.</item>
///     </list>
/// </para>
/// </summary>
public class Demo04_WaitAndRetryForever : DemoBase
{
    public override string Description =>
        "This demo also retries enough to always ensure success. But we haven't had to 'guess' how many retries were necessary. We just said: wait-and-retry-forever.";

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
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
            MaxRetryAttempts = int.MaxValue, // Retry indefinitely
            Delay = TimeSpan.FromMilliseconds(200),
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
                await strategy.ExecuteAsync(async token =>
                {
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
