using System.Diagnostics;
using Polly.CircuitBreaker;

using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Demonstrates using a Retry, a CircuitBreaker and two Fallback strategies. <br/>
///     Same as Demo07 but now uses Fallback strategies to provide substitute values, when the call still fails overall.
/// </para>
/// <para>
///     Loops through a series of HTTP requests, keeping track of each requested <br/>
///     item and reporting server failures when encountering exceptions.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>operation is identical to Demo06 and Demo07</item>
///         <item>except fallback strategies provide nice substitute messages, if still fails overall</item>
///         <item>OnFallback delegate captures the stats that were captured in try/catches in demos 06 and 07</item>
///         <item>also demonstrates how you can use the same kind of strategy <br/>(Fallback in this case) twice (or more) in a pipeline.</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: ... request #N(...)": Response received on time.</item>
///         <item>"Fallback catches failed with: ...": Shortcut request due to broken circuit is caught by the fallback.</item>
///         <item>"Response: Please try again later [message ...": Shortcut request's response is substituted.</item>
///     </list>
/// </para>
/// </summary>
public class Demo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker : DemoBase
{
    private int eventualFailuresDueToCircuitBreaking;
    private int eventualFailuresForOtherReasons;

    public override string Description =>
        "This demo matches 06 and 07 (retry with circuit-breaker), but also introduces Fallbacks: we can provide graceful fallback messages, on overall failure.";

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        EventualSuccesses = 0;
        Retries = 0;
        eventualFailuresDueToCircuitBreaking = 0;
        eventualFailuresForOtherReasons = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        Stopwatch? watch = null;

        // Provide the return type (string) to be able to use Fallback.
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

        // Define a fallback strategy: provide a substitute message to the user, if we found the circuit was broken.
        pipelineBuilder.AddFallback(new()
        {
            ShouldHandle = new PredicateBuilder<string>().Handle<BrokenCircuitException>(),
            FallbackAction = args => Outcome.FromResultAsValueTask("Please try again later [message substituted by fallback strategy]"),
            OnFallback = args =>
            {
                watch!.Stop();
                var exception = args.Outcome.Exception!;
                progress.Report(ProgressWithMessage($"Fallback catches failed with: {exception.Message} (after {watch.ElapsedMilliseconds}ms)", Color.Red));
                eventualFailuresDueToCircuitBreaking++;
                return default;
            }
        });

        pipelineBuilder.AddRetry(new()
        {
            // Since pipeline has a string type parameter that's why the PredicateBuilder has to have one as well.
            ShouldHandle = new PredicateBuilder<string>().Handle<Exception>(ex => ex is not BrokenCircuitException),
            MaxRetryAttempts = int.MaxValue,
            Delay = TimeSpan.FromMilliseconds(200),
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
                progress.Report(ProgressWithMessage($"Strategy logging: {exception.Message}", Color.Yellow));
                Retries++;
                return default;
            }
        });

        pipelineBuilder.AddCircuitBreaker(new()
        {
            // Since pipeline has a string type parameter that's why the PredicateBuilder has to have one as well.
            ShouldHandle = new PredicateBuilder<string>().Handle<Exception>(),
            FailureRatio = 1.0,
            SamplingDuration = TimeSpan.FromSeconds(2),
            MinimumThroughput = 4,
            BreakDuration = TimeSpan.FromSeconds(3),
            OnOpened = args =>
            {
                progress.Report(ProgressWithMessage(
                        $".Breaker logging: Breaking the circuit for {args.BreakDuration.TotalMilliseconds}ms!",
                        Color.Magenta));

                var exception = args.Outcome.Exception!;
                progress.Report(ProgressWithMessage($"..due to: {exception.Message}", Color.Magenta));
                return default;
            },
            OnClosed = args =>
            {
                progress.Report(ProgressWithMessage(".Breaker logging: Call OK! Closed the circuit again!", Color.Magenta));
                return default;
            },
            OnHalfOpened = args =>
            {
                progress.Report(ProgressWithMessage(".Breaker logging: Half-open: Next call is a trial!", Color.Magenta));
                return default;
            }
        });

        // Build the pipeline which now composes four strategies (from inner to outer):
        // Circuit Breaker
        // Retry
        // Fallback for open circuit
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
                progress.Report(ProgressWithMessage($"Response : {responseBody} (after {watch.ElapsedMilliseconds}ms)", Color.Green));
                EventualSuccesses++;
            }
            // This try-catch is not needed, since we have a Fallback for any Exceptions.
            // It's only been left in to *demonstrate* it should never get hit.
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
        new("Requests failed early by broken circuit", eventualFailuresDueToCircuitBreaking, Color.Magenta),
        new("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
    };
}
