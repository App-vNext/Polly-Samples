using System.Collections.Concurrent;

using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Same scenario as previous demo:
///     <list type="bullet">
///         <item>Imagine a microservice or web front end (the upstream caller) trying to call two endpoints on a downstream system.</item>
///         <item>The 'good' endpoint responds quickly. The 'faulting' endpoint faults, and responds slowly.</item>
///         <item>Imagine the caller has limited capacity (all single instances of services/webapps eventually hit some capacity limit).</item>
///     </list>
/// </para>
/// <para>
///     Compared to demo 10, this demo isolates the calls
///     to the 'good' and 'faulting' endpoints in separate concurrency limiters. <br/>
///     A random combination of calls to the 'good' and 'faulting' endpoint are made.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>Because the separate 'good' and 'faulting' streams are isolated in separate concurrency limiters,</item>
///         <item>the 'faulting' calls still back up (high pending and failing number),</item>
///         <item>but 'good' calls (in a separate limiter) are <strong>unaffected</strong> (all succeed; none pending or failing).</item>
///     </list>
/// </para>
/// <para>
///     Concurrency limiters can be used to implement the bulkhead resiliency pattern. <br/>
///     Bulkheads' motto: making sure one fault doesn't sink the whole ship!
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: Fast ... to request #N": Response received from nonthrottledgood route.</item>
///         <item>"Response: Slow... to request #N": Response received from nonthrottledfaulting route.</item>
///         <item>"Request N failed with: ... rate limiter": Concurrency limit is reached, request is rejected.</item>
///     </list>
/// </para>
/// </summary>
public class Demo11_MultipleConcurrencyLimiters : ConcurrencyLimiterDemoBase
{
     // Let's imagine this caller has some theoretically limited capacity.
    const int callerParallelCapacity = 8; // artificially low - but easier to follow to illustrate the principle

    private readonly ResiliencePipeline limiterForGoodCalls = new ResiliencePipelineBuilder()
        .AddConcurrencyLimiter(
            permitLimit: callerParallelCapacity / 2,
            queueLimit: 10)
        .Build();

    private readonly ResiliencePipeline limiterForFaultingCalls = new ResiliencePipelineBuilder()
        .AddConcurrencyLimiter(
            permitLimit: callerParallelCapacity / 2,
            queueLimit: 10)
        .Build();

    public override string Description =>
        "Demonstrates a good call stream and faulting call stream separated into separate concurrency limiters. The faulting call stream is isolated from affecting the good call stream.";

    public override async Task ExecuteAsync(CancellationToken externalCancellationToken, IProgress<DemoProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(nameof(progress));

        PrintHeader(progress);
        TotalRequests = 0;

        await ValueTask.FromResult(true);

        var tasks = new List<Task>();
        var internalCancellationTokenSource = new CancellationTokenSource();
        var combinedToken = CancellationTokenSource
            .CreateLinkedTokenSource(externalCancellationToken, internalCancellationTokenSource.Token)
            .Token;

        var messages = new ConcurrentQueue<(string Message, Color Color)>();
        var client = new HttpClient();
        var internalCancel = false;

        while (!(internalCancel || externalCancellationToken.IsCancellationRequested))
        {
            TotalRequests++;
            var thisRequest = TotalRequests;

            if (Random.Shared.Next(0, 2) == 0)
            {
                GoodRequestsMade++;
                tasks.Add(CallGoodEndpoint(client, messages, thisRequest, combinedToken));
            }
            else
            {
                FaultingRequestsMade++;
                tasks.Add(CallFaultingEndpoint(client, messages, thisRequest, combinedToken));
            }

            while (messages.TryDequeue(out var tuple))
            {
                progress.Report(ProgressWithMessage(tuple.Message, tuple.Color));
            }

            await Task.Delay(TimeSpan.FromSeconds(0.2), externalCancellationToken).ConfigureAwait(false);
            internalCancel = ShouldTerminateByKeyPress();
        }
    }

    private Task CallFaultingEndpoint(HttpClient client, ConcurrentQueue<(string Message, Color Color)> messages, int thisRequest, CancellationToken cancellationToken)
    {
        ValueTask issueRequest = limiterForFaultingCalls.ExecuteAsync(async token =>
        {
            try
            {
                var responseBody = await IssueFaultingRequestAndProcessResponseAsync(client, token).ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    messages.Enqueue(($"Response: {responseBody}", Color.Green));
                }
                FaultingRequestsSucceeded++;
            }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested)
                {
                    messages.Enqueue(($"Request {thisRequest} eventually failed with: {e.Message}", Color.Red));
                }
                FaultingRequestsFailed++;
            }
        }, cancellationToken);

        Task handleFailure = issueRequest
            .AsTask()
            .ContinueWith((failedTask, state) =>
            {
                if (failedTask.IsFaulted)
                {
                    var message = $"Request {state} failed with: {failedTask.Exception!.Flatten().InnerExceptions.First().Message}";
                    messages.Enqueue((message, Color.Red));
                }
                FaultingRequestsFailed++;
            }, thisRequest, TaskContinuationOptions.NotOnRanToCompletion);

        return handleFailure;
    }

    private Task CallGoodEndpoint(HttpClient client, ConcurrentQueue<(string Message, Color Color)> messages, int thisRequest, CancellationToken cancellationToken)
    {
        ValueTask issueRequest = limiterForGoodCalls.ExecuteAsync(async token =>
        {
            try
            {
                var responseBody = await IssueGoodRequestAndProcessResponseAsync(client, token).ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    messages.Enqueue(($"Response: {responseBody}", Color.Green));
                }
                GoodRequestsSucceeded++;
            }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested)
                {
                    messages.Enqueue(($"Request {thisRequest} eventually failed with: {e.Message}", Color.Red));
                }
                GoodRequestsFailed++;
            }
        }, cancellationToken);

        Task handleFailure = issueRequest
            .AsTask()
            .ContinueWith((failedTask, state) =>
            {
                if (failedTask.IsFaulted)
                {
                    var message = $"Request {state} failed with: {failedTask.Exception!.Flatten().InnerExceptions.First().Message}";
                    messages.Enqueue((message, Color.Red));
                }
                GoodRequestsFailed++;
            }, thisRequest, TaskContinuationOptions.NotOnRanToCompletion);

        return handleFailure;
    }

    public override Statistic[] LatestStatistics => new Statistic[]
    {
        new("Total requests made", TotalRequests, Color.Default),
        new("Good endpoint: requested", GoodRequestsMade, Color.Default),
        new("Good endpoint: succeeded", GoodRequestsSucceeded, Color.Green),
        new("Good endpoint: pending", GoodRequestsMade - GoodRequestsSucceeded - GoodRequestsFailed, Color.Yellow),
        new("Good endpoint: failed", GoodRequestsFailed, Color.Red),
        new("Faulting endpoint: requested", FaultingRequestsMade, Color.Default),
        new("Faulting endpoint: succeeded", FaultingRequestsSucceeded, Color.Green),
        new("Faulting endpoint: pending", FaultingRequestsMade - FaultingRequestsSucceeded - FaultingRequestsFailed, Color.Yellow),
        new("Faulting endpoint: failed", FaultingRequestsFailed, Color.Red),
    };
}
