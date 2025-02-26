using System.Diagnostics;
using Polly.Timeout;
using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Demonstrates using a Retry, a Timeout and two Fallback strategies. <br/>
///     In this demo, the delay in the retry is deliberately so long that the timeout strategy wrapping it will time it out <br/>
///     (in lieu for now of a demo server endpoint responding slowly).
/// </para>
/// <para>
///     Loops through a series of HTTP requests, keeping track of each requested <br/>
///     item and reporting server failures when encountering exceptions.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>though the console logs that a retry will be made, the 4-second wait before the retry <br/>
///         is preemptively timed-out by the two-second timeout</item>
///         <item>a fallback strategy then provides substitute message for the user</item>
///         <item>otherwise similar to demo08.</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: ... request #N(...)": Response received on time.</item>
///         <item>"Fallback catches failed with: ...": Preemptively timed-out request is caught by the fallback.</item>
///         <item>"Response: Please try again later [Fallback for timeout]": Preemptively timed-out request's response is substituted.</item>
///     </list>
/// </para>
/// </summary>
public class Demo09_Pipeline_Fallback_Timeout_WaitAndRetry : DemoBase
{
    private int eventualFailuresDueToTimeout;
    private int eventualFailuresForOtherReasons;

    public override string Description =>
        "Demonstrates introducing a Timeout strategy. The timeout will eventually time-out on the retries. When we timeout, we again use Fallback strategies to substitute more graceful messages.";

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        EventualSuccesses = 0;
        Retries = 0;
        eventualFailuresDueToTimeout = 0;
        eventualFailuresForOtherReasons = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        Stopwatch? watch = null;
        var pipelineBuilder = new ResiliencePipelineBuilder<string>();

        // Define a fallback strategy: provide a substitute message to the user, for any exception.
        pipelineBuilder.AddFallback(new()
        {
            ShouldHandle = new PredicateBuilder<string>().Handle<Exception>(),
            FallbackAction = args => Outcome.FromResultAsValueTask("Please try again later [Fallback for any exception]"),
            OnFallback = args =>
            {
                watch!.Stop();
                var exception = args.Outcome.Exception!;
                progress.Report(ProgressWithMessage($"Fallback catches eventually failed with: {exception.Message} (after {watch.ElapsedMilliseconds}ms)", Color.Red));
                eventualFailuresForOtherReasons++;
                return default;
            }
        });

        // Define a fallback strategy: provide a substitute message to the user, if we found the call was rejected due to timeout.
        pipelineBuilder.AddFallback(new()
        {
            ShouldHandle = new PredicateBuilder<string>().Handle<TimeoutRejectedException>(),
            FallbackAction = args => Outcome.FromResultAsValueTask("Please try again later [Fallback for timeout]"),
            OnFallback = args =>
            {
                watch!.Stop();
                var exception = args.Outcome.Exception!;
                progress.Report(ProgressWithMessage($"Fallback catches failed with: {exception.Message} (after {watch.ElapsedMilliseconds}ms)", Color.Red));
                eventualFailuresDueToTimeout++;
                return default;
            }
        });

        // Define our timeout strategy: time out after 2 seconds.
        pipelineBuilder.AddTimeout(new TimeoutStrategyOptions()
        {
            Timeout = TimeSpan.FromSeconds(2),
            OnTimeout = args =>
            {
                var logMessage = $".The task was terminated because it ran out of time. Time cap was {args.Timeout.TotalSeconds}s";
                progress.Report(ProgressWithMessage(logMessage, Color.Yellow));
                return default;
            }
        });

        // Define our retry strategy: keep retrying with 4 second gaps. This is (intentionally) too long: to demonstrate that the timeout strategy will time out on this before waiting for the retry.
        pipelineBuilder.AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder<string>().Handle<Exception>(),
            Delay = TimeSpan.FromSeconds(4),
            MaxRetryAttempts = int.MaxValue,
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
                progress.Report(ProgressWithMessage($".Log,then retry: {exception.Message}", Color.Yellow));
                Retries++;
                return default;
            }
        });

        // Build the pipeline which now composes four strategies (from inner to outer):
        // Retry
        // Timeout
        // Fallback for timeout
        // Fallback for any other exception
        var pipeline = pipelineBuilder.Build();

        var client = new HttpClient();
        var internalCancel = false;

        while (!(internalCancel || cancellationToken.IsCancellationRequested))
        {
            TotalRequests++;
            watch = Stopwatch.StartNew();

            try
            {
                var responseBody = await pipeline.ExecuteAsync(async token =>
                    await IssueRequestAndProcessResponseAsync(client, token), cancellationToken);

                watch.Stop();
                progress.Report(ProgressWithMessage($"Response: {responseBody}(after {watch.ElapsedMilliseconds}ms)", Color.Green));
                EventualSuccesses++;
            }
            catch (Exception e)
            {
                var errorMessage = "Should never arrive here. Use of fallback for any Exception should have provided nice fallback value for exceptions.";
                throw new UnreachableException(errorMessage, e);
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
        new("Requests timed out by timeout strategy", eventualFailuresDueToTimeout, Color.Magenta),
        new("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
    };
}
