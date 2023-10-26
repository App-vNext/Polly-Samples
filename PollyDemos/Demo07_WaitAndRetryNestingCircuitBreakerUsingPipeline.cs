using System.Diagnostics;
using Polly.CircuitBreaker;

using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Demonstrates using the Retry and CircuitBreaker strategies.<br/>
///     Same as Demo06 but this time combines the strategies by using ResiliencePipelineBuilder.
/// </para>
/// <para>
///     Loops through a series of HTTP requests, keeping track of each requested <br/>
///     item and reporting server failures when encountering exceptions.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>The operation is identical to Demo06.</item>
///         <item>The code demonstrates how using the ResiliencePipelineBuilder <br/>
///         makes your combined pipeline more concise, at the point of execution.</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: ... request #N(...)": Response received on time.</item>
///         <item>"Request N failed with: BrokenCircuitException": Request is shortcut due to broken circuit.</item>
///     </list>
/// </para>
/// </summary>
public class Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline : DemoBase
{
    private int eventualFailuresDueToCircuitBreaking;
    private int eventualFailuresForOtherReasons;

    public override string Description =>
        "This demonstrates CircuitBreaker (see Demo06), but uses the ResiliencePipelineBuilder to compose the strategies. Only the underlying code differs.";

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        EventualSuccesses = 0;
        Retries = 0;
        eventualFailuresDueToCircuitBreaking = 0;
        eventualFailuresForOtherReasons = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        // Define a pipeline builder which will be used to compose strategies incrementally.
        var pipelineBuilder = new ResiliencePipelineBuilder();

        pipelineBuilder.AddRetry(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not BrokenCircuitException),
            MaxRetryAttempts = int.MaxValue,
            Delay = TimeSpan.FromMilliseconds(200),
            OnRetry = args =>
            {
                var exception = args.Outcome.Exception!;
                progress.Report(ProgressWithMessage($"Strategy logging: {exception.Message}", Color.Yellow));
                Retries++;
                return default;
            }
        }); // We are not calling the Build method here because we will do it as a separate step to make the code cleaner.

        pipelineBuilder.AddCircuitBreaker(new()
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
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
        }); // We are not calling the Build method because we want to add one more strategy to the pipeline.

        // Build the pipeline since we have added all the necessary strategies to it.
        var pipeline = pipelineBuilder.Build();

        var client = new HttpClient();
        var internalCancel = false;

        while (!(internalCancel || cancellationToken.IsCancellationRequested))
        {
            TotalRequests++;
            var watch = Stopwatch.StartNew();

            try
            {
                // Manage the call according to the pipeline.
                var responseBody = await pipeline.ExecuteAsync(async token =>
                {
                    // This code is executed through both strategies in the pipeline:
                    // Retry is the outer, and circuit breaker is the inner.
                    // Demo 06 shows a decomposed version of what this is equivalent to.

                    return await IssueRequestAndProcessResponseAsync(client, token);
                }, cancellationToken);

                watch.Stop();
                progress.Report(ProgressWithMessage($"Response : {responseBody} (after {watch.ElapsedMilliseconds}ms)", Color.Green));
                EventualSuccesses++;
            }
            catch (BrokenCircuitException bce)
            {
                watch.Stop();
                var logMessage = $"Request {TotalRequests} failed with: {bce.GetType().Name} (after {watch.ElapsedMilliseconds}ms)";
                progress.Report(ProgressWithMessage(logMessage, Color.Red));
                eventualFailuresDueToCircuitBreaking++;
            }
            catch (Exception e)
            {
                watch.Stop();
                var logMessage = $"Request {TotalRequests} eventually failed with: {e.Message} (after {watch.ElapsedMilliseconds}ms)";
                progress.Report(ProgressWithMessage(logMessage, Color.Red));
                eventualFailuresForOtherReasons++;
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
