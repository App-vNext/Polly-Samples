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
///         <item>We now have waits among the retries.</item>
///         <item>In this case, still not enough wait / not enough retries <br/>
///         for the underlying system to have recovered.</item>
///         <item>So we still fail some calls.</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: ... request #N(...)": Response received on time.</item>
///         <item>"Request N eventually failed ... (Too Many Requests)": Request runs out of retry attempts, shows the rate limited response.</item>
///     </list>
/// </para>
/// </summary>
public class Demo02_WaitAndRetryNTimes : DemoBase
{
    public override string Description =>
        "Compared to previous demo, this adds waits between the retry attempts. Not always enough wait to ensure success, tho.";

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
