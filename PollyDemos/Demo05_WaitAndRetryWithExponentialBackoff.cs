using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Demonstrates Retry strategy with calculated retry delays to back off. <br/>
///     Loops through a series of HTTP requests, keeping track of each requested <br/>
///     item and reporting server failures when encountering exceptions.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>All calls still succeed! Yay!</item>
///         <item>But we didn't hammer the underlying server so hard - we backed off.</item>
///         <item>That's healthier for it, if it might be struggling ...<br/>
///         ... and if a lot of clients might be doing this simultaneously.<br/>
///         ... What if the underlying system was totally down tho?<br/>
///         ... Keeping trying forever would be counterproductive (so, see Demo06)</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: ... request #N(...)": Response received on time.</item>
///     </list>
/// </para>
/// </summary>
public class Demo05_WaitAndRetryWithExponentialBackoff : DemoBase
{
    public override string Description =>
        "This demonstrates exponential back-off. We have enough retries to ensure success. But we don't hammer the server so hard: we increase the delay between each try.";

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
            MaxRetryAttempts = 6, // We could also retry indefinitely by using int.MaxValue
            BackoffType = DelayBackoffType.Exponential, // Back off: 1s, 2s, 4s, 8s, ... + jitter
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
                progress.Report(ProgressWithMessage($"Strategy logging: {exception.Message}", Color.Yellow));
                progress.Report(ProgressWithMessage($" ... automatically delaying for {args.RetryDelay.TotalMilliseconds}ms.", Color.Yellow));
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
