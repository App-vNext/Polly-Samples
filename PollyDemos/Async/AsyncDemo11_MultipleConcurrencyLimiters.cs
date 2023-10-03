using System.Collections.Concurrent;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
    /// <summary>
    /// Same scenario as previous demo:
    /// Imagine a microservice or web front end (the upstream caller) trying to call two endpoints on a downstream system.
    /// The 'good' endpoint responds quickly.  The 'faulting' endpoint faults, and responds slowly.
    /// Imagine the _caller_ has limited capacity (all single instances of services/webapps eventually hit some capacity limit).
    ///
    /// Compared to demo 10, this demo 11 isolates the calls
    /// to the 'good' and 'faulting' endpoints in separate concurrency limiters.
    /// A random combination of calls to the 'good' and 'faulting' endpoint are made.
    ///
    /// Observations:
    /// Because the separate 'good' and 'faulting' streams are isolated in separate concurrency limiters,
    /// the 'faulting' calls still back up (high pending and failing number), but
    /// 'good' calls (in a separate concurrency limiter) are *unaffected* (all succeed; none pending or failing).
    ///
    /// Concurrency limiters can be used to implement the bulkhead resiliency pattern.
    /// Bulkheads' motto: making sure one fault doesn't sink the whole ship!
    /// </summary>
    public class AsyncDemo11_MultipleConcurrencyLimiters : AsyncConcurrencyLimiterDemo
    {
         // Let's imagine this caller has some theoretically limited capacity.
        const int callerParallelCapacity = 8; // artificially low - but easier to follow to illustrate the principle

        private readonly ResiliencePipeline limiterForGoodCalls = new ResiliencePipelineBuilder()
            .AddConcurrencyLimiter(
                permitLimit: callerParallelCapacity / 2,
                queueLimit: 10)
            .Build();

        private readonly ResiliencePipeline limiterForFaultingCalls =
            new ResiliencePipelineBuilder()
            .AddConcurrencyLimiter(
                permitLimit: callerParallelCapacity / 2,
                queueLimit: 10)
            .Build();

        public override string Description =>
            "Demonstrates a good call stream and faulting call stream separated into separate concurrency limiters. The faulting call stream is isolated from affecting the good call stream.";

        public override async Task ExecuteAsync(CancellationToken externalCancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(nameof(progress));

            PrintHeader(progress, nameof(AsyncDemo11_MultipleConcurrencyLimiters));
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
            ValueTask issueRequest = limiterForGoodCalls.ExecuteAsync(async token =>
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
            ValueTask issueRequest = limiterForGoodCalls.ExecuteAsync(async token =>
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
