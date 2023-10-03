using System.Collections.Concurrent;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
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
                    permitLimit: 8,  // (artificially low - but easier to follow to illustrate the principle)
                    queueLimit: 1)
                .Build();

        public override string Description =>
            "Demonstrates a good call stream and faulting call stream sharing resources. Good call stream throughput is blocked by the faulting call stream.";

        public override async Task ExecuteAsync(CancellationToken externalCancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            PrintHeader(progress, nameof(AsyncDemo10_SharedConcurrencyLimiter));
            totalRequests = 0;

            await Task.FromResult(true).ConfigureAwait(false); // Ensure none of what follows runs synchronously.

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
                totalRequests++;
                var thisRequest = totalRequests;

                // Randomly make either 'good' or 'faulting' calls.
                if (Random.Shared.Next(0, 2) == 0)
                {
                    goodRequestsMade++;
                    tasks.Add(CallGoodEndpoint(client, messages, thisRequest, combinedToken));
                }
                else
                {
                    faultingRequestsMade++;
                    tasks.Add(CallFaultingEndpoint(client, messages, thisRequest, combinedToken));
                }

                AddSummary(messages);

                // Output all messages available right now.
                while (messages.TryDequeue(out var tuple))
                {
                    progress.Report(ProgressWithMessage(tuple.Message, tuple.Color));
                }

                // Wait briefly
                await Task.Delay(TimeSpan.FromSeconds(0.2), externalCancellationToken).ConfigureAwait(false);
                // Support cancellation by keyboard, when called from a console; ignore exceptions, if console not accessible.
                try
                {
                    internalCancel = Console.KeyAvailable;
                }
                catch
                {
                }
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

        private void AddSummary(ConcurrentQueue<(string Message, Color Color)> messages)
        {
            messages.Enqueue(($"Total requests: requested {totalRequests:00}, ", Color.White));
            messages.Enqueue(($"Good endpoint: requested {goodRequestsMade:00}, ", Color.White));
            messages.Enqueue(($"Good endpoint:succeeded {goodRequestsSucceeded:00}, ", Color.Green));
            messages.Enqueue(($"Good endpoint:pending {goodRequestsMade - goodRequestsSucceeded - goodRequestsFailed:00}, ", Color.Yellow));
            messages.Enqueue(($"Good endpoint:failed {goodRequestsFailed:00}.", Color.Red));
            messages.Enqueue((string.Empty, Color.Default));

            messages.Enqueue(($"Faulting endpoint: requested {faultingRequestsMade:00}, ", Color.White));
            messages.Enqueue(($"Faulting endpoint:succeeded {faultingRequestsSucceeded:00}, ", Color.Green));
            messages.Enqueue(($"Faulting endpoint:pending {faultingRequestsMade - faultingRequestsSucceeded - faultingRequestsFailed:00}, ", Color.Yellow));
            messages.Enqueue(($"Faulting endpoint:failed {faultingRequestsFailed:00}.", Color.Red));
            messages.Enqueue((string.Empty, Color.Default));
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
                    faultingRequestsSucceeded++;
                }
                catch (Exception e)
                {
                    if (!token.IsCancellationRequested)
                    {
                        messages.Enqueue(($"Request {thisRequest} eventually failed with: {e.Message}", Color.Red));
                    }
                    faultingRequestsFailed++;
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
                    faultingRequestsFailed++;
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
                    goodRequestsSucceeded++;
                }
                catch (Exception e)
                {
                    if (!token.IsCancellationRequested)
                    {
                        messages.Enqueue(($"Request {thisRequest} eventually failed with: {e.Message}", Color.Red));
                    }
                    goodRequestsFailed++;
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
                    goodRequestsFailed++;
                }, thisRequest, TaskContinuationOptions.NotOnRanToCompletion);

            return handleFailure;
        }

        public override Statistic[] LatestStatistics => new Statistic[]
        {
            new("Total requests made", totalRequests, Color.Default),
            new("Good endpoint: requested", goodRequestsMade, Color.Default),
            new("Good endpoint: succeeded", goodRequestsSucceeded, Color.Green),
            new("Good endpoint: pending", goodRequestsMade - goodRequestsSucceeded - goodRequestsFailed, Color.Yellow),
            new("Good endpoint: failed", goodRequestsFailed, Color.Red),
            new("Faulting endpoint: requested", faultingRequestsMade, Color.Default),
            new("Faulting endpoint: succeeded", faultingRequestsSucceeded, Color.Green),
            new("Faulting endpoint: pending", faultingRequestsMade - faultingRequestsSucceeded - faultingRequestsFailed, Color.Yellow),
            new("Faulting endpoint: failed", faultingRequestsFailed, Color.Red),
        };
    }
}
