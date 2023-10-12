using System.Collections.Concurrent;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async;

/// <summary>
/// Imagine a microservice or web front end (the upstream caller) trying to call two endpoints on a downstream system.
/// The 'good' endpoint responds quickly.  The 'faulting' endpoint either faults or responds slowly.
/// Imagine the _caller_ has limited capacity (all single instances of upstream services/webapps eventually hit some capacity limit).
///
/// This demo 10 does not separate call streams into separate concurrency limiters.
/// A random combination of calls to the 'good' and 'faulting' endpoint are made.
///
/// Observations:
/// Because concurrency limiters do not isolate the separate streams of calls,
/// eventually all the caller's capacity is taken up waiting on the 'faulting' downstream calls.
/// So the performance of 'good' calls is starved of resource, and starts suffering too.
/// Watch how the number of pending and failing calls to the good endpoint also climbs,
/// as the faulting calls saturate all resources in the caller.
/// </summary>
public class AsyncDemo10_SharedConcurrencyLimiter : AsyncConcurrencyLimiterDemo
{
    // Let's imagine this caller has some theoretically limited capacity so that *it* will suffer capacity-starvation if the downstream system is faulting.
    // In demo 10, all calls share the same concurrency limiter.
    private readonly ResiliencePipeline sharedLimiter = new ResiliencePipelineBuilder()
            .AddConcurrencyLimiter(
                permitLimit: 8,  // artificially low - but easier to follow to illustrate the principle
                queueLimit: 1)
            .Build();

    public override string Description =>
        "Demonstrates a good call stream and faulting call stream sharing resources. Good call stream throughput is blocked by the faulting call stream.";

    public override async Task ExecuteAsync(CancellationToken externalCancellationToken, IProgress<DemoProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        PrintHeader(progress);
        TotalRequests = 0;

        await ValueTask.FromResult(true); // Ensure none of what follows runs synchronously.

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

            // Output all messages available right now.
            while (messages.TryDequeue(out var tuple))
            {
                progress.Report(ProgressWithMessage(tuple.Message, tuple.Color));
            }

            // Wait briefly
            await Task.Delay(TimeSpan.FromSeconds(0.2), externalCancellationToken).ConfigureAwait(false);
            internalCancel = ShouldTerminateByKeyPress();
        }

        // Cancel any unstarted and running tasks.
        internalCancellationTokenSource.Cancel();
        try
        {
            Task.WaitAll(tasks.ToArray());
        }
        catch
        {
            // Swallow any shutdown exceptions eg TaskCanceledException - we don't care - we are shutting down the demo.
        }
    }

    private Task CallFaultingEndpoint(HttpClient client, ConcurrentQueue<(string Message, Color Color)> messages, int thisRequest, CancellationToken cancellationToken)
    {
        ValueTask issueRequest = sharedLimiter.ExecuteAsync(async token =>
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
                    // When the ConcurrencyLimiter kicks in then the AggregateException will contain a RateLimiterRejectedException
                    var message = $"Request {state} failed with: {failedTask.Exception!.Flatten().InnerExceptions.First().Message}";
                    messages.Enqueue((message, Color.Red));
                }
                FaultingRequestsFailed++;
            }, thisRequest, TaskContinuationOptions.NotOnRanToCompletion);

        return handleFailure;
    }

    private Task CallGoodEndpoint(HttpClient client, ConcurrentQueue<(string Message, Color Color)> messages, int thisRequest, CancellationToken cancellationToken)
    {
        ValueTask issueRequest = sharedLimiter.ExecuteAsync(async token =>
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
                    // When the ConcurrencyLimiter kicks in then the AggregateException will contain a RateLimiterRejectedException
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
