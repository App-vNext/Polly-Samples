using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Bulkhead;
using Polly.Timeout;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
    /// <summary>
    /// Imagine a microservice or web front end (the upstream caller) trying to call two endpoints on a downstream system.
    /// The 'good' endpoint responds quickly.  The 'faulting' endpoint faults, and responds slowly.
    /// Imagine the _caller_ has limited capacity (all single instances of upstream services/webapps eventually hit some capacity limit).
    /// 
    /// This demo 00 does not separate call streams into separate bulkheads.  
    /// A random combination of calls to the 'good' and 'faulting' endpoint are made.
    /// 
    /// Observe: --
    /// Because bulkheads do not isolate the separate streams of calls, 
    /// eventually all the caller's capacity is taken up waiting on the 'faulting' downstream calls.
    /// So the performance of 'good' calls is starved of resource, and starts suffering too.
    /// Watch how the number of pending and failing calls to the good endpoint also climbs,
    /// as the faulting calls saturate all resource in the caller.
    /// </summary>
    public class BulkheadAsyncDemo00_NoIsolation : AsyncDemo
    {
        // Track the number of 'good' and 'faulting' requests made, succeeded and failed.
        // At any time, requests pending = made - succeeded - failed.
        private int totalRequests = 0;
        private int goodRequestsMade = 0;
        private int goodRequestsSucceeded = 0;
        private int goodRequestsFailed = 0;
        private int faultingRequestsMade = 0;
        private int faultingRequestsSucceeded = 0;
        private int faultingRequestsFailed = 0;

        public override string Description =>
            "Demonstrates a good call stream and faulting call stream sharing resources.  Good call stream throughput is blocked by the faulting call stream.";

        public override async Task ExecuteAsync(CancellationToken externalCancellationToken,
            IProgress<DemoProgress> progress)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));

            progress.Report(ProgressWithMessage(nameof(BulkheadAsyncDemo00_NoIsolation)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            // Let's imagine this caller has some theoretically limited capacity, so that *it* will suffer capacity-starvation, if the downstream system is faulting.
            const int callerParallelCapacity = 8; // (artificially low - but easier to follow, to illustrate principle)
            // In demo 00, all calls share the same bulkhead.
            var sharedBulkhead = Policy.BulkheadAsync(callerParallelCapacity, 1);

            var rand = new Random();
            totalRequests = 0;

            await Task.FromResult(true).ConfigureAwait(false); // Ensure none of what follows runs synchronously.

            IList<Task> tasks = new List<Task>();
            var internalCancellationTokenSource = new CancellationTokenSource();
            var combinedToken = CancellationTokenSource
                .CreateLinkedTokenSource(externalCancellationToken, internalCancellationTokenSource.Token).Token;

            var messages = new ConcurrentQueue<ColoredMessage>();

            using (var client = new HttpClient())
            {
                var internalCancel = false;
                while (!internalCancel && !externalCancellationToken.IsCancellationRequested)
                {
                    totalRequests++;

                    var thisRequest = totalRequests;

                    // Randomly make either 'good' or 'faulting' calls.
                    if (rand.Next(0, 2) == 0)
                    {
                        goodRequestsMade++;
                        // Call 'good' endpoint.
                        tasks.Add(sharedBulkhead.ExecuteAsync(async ct =>
                            {
                                try
                                {
                                    // Make a request and get a response, from the good endpoint
                                    var msg = await (await client
                                            .GetAsync(
                                                Configuration.WEB_API_ROOT + "/api/nonthrottledgood/" + totalRequests,
                                                ct).ConfigureAwait(false)).Content.ReadAsStringAsync()
                                        .ConfigureAwait(false);
                                    if (!ct.IsCancellationRequested)
                                        messages.Enqueue(new ColoredMessage($"Response: {msg}", Color.Green));

                                    goodRequestsSucceeded++;
                                }
                                catch (Exception e)
                                {
                                    if (!ct.IsCancellationRequested)
                                        messages.Enqueue(new ColoredMessage(
                                            $"Request {thisRequest} eventually failed with: {e.Message}", Color.Red));

                                    goodRequestsFailed++;
                                }
                            }, combinedToken)
                            .ContinueWith((t, k) =>
                            {
                                if (t.IsFaulted)
                                    messages.Enqueue(new ColoredMessage(
                                        $"Request {k} failed with: {t.Exception.Flatten().InnerExceptions.First().Message}",
                                        Color.Red));

                                goodRequestsFailed++;
                            }, thisRequest, TaskContinuationOptions.NotOnRanToCompletion)
                        );
                    }
                    else
                    {
                        faultingRequestsMade++;
                        // call 'faulting' endpoint.
                        tasks.Add(sharedBulkhead.ExecuteAsync(async ct =>
                            {
                                try
                                {
                                    // Make a request and get a response, from the faulting endpoint
                                    var msg = await (await client
                                            .GetAsync(
                                                Configuration.WEB_API_ROOT + "/api/nonthrottledfaulting/" +
                                                totalRequests,
                                                ct).ConfigureAwait(false)).Content.ReadAsStringAsync()
                                        .ConfigureAwait(false);
                                    if (!combinedToken.IsCancellationRequested)
                                        messages.Enqueue(new ColoredMessage($"Response: {msg}", Color.Green));

                                    faultingRequestsSucceeded++;
                                }
                                catch (Exception e)
                                {
                                    if (!ct.IsCancellationRequested)
                                        messages.Enqueue(new ColoredMessage(
                                            $"Request {thisRequest} eventually failed with: {e.Message}", Color.Red));

                                    faultingRequestsFailed++;
                                }
                            }, combinedToken)
                            .ContinueWith((t, k) =>
                            {
                                if (t.IsFaulted)
                                    messages.Enqueue(new ColoredMessage(
                                        $"Request {k} failed with: {t.Exception.Flatten().InnerExceptions.First().Message}",
                                        Color.Red));

                                faultingRequestsFailed++;
                            }, thisRequest, TaskContinuationOptions.NotOnRanToCompletion)
                        );
                    }

                    messages.Enqueue(new ColoredMessage($"Total requests: requested {totalRequests:00}, ",
                        Color.White));
                    messages.Enqueue(new ColoredMessage($"Good endpoint: requested {goodRequestsMade:00}, ",
                        Color.White));
                    messages.Enqueue(new ColoredMessage($"Good endpoint:succeeded {goodRequestsSucceeded:00}, ",
                        Color.Green));
                    messages.Enqueue(new ColoredMessage(
                        $"Good endpoint:pending {goodRequestsMade - goodRequestsSucceeded - goodRequestsFailed:00}, ",
                        Color.Yellow));
                    messages.Enqueue(new ColoredMessage($"Good endpoint:failed {goodRequestsFailed:00}.", Color.Red));

                    messages.Enqueue(new ColoredMessage(string.Empty));
                    messages.Enqueue(new ColoredMessage($"Faulting endpoint: requested {faultingRequestsMade:00}, ",
                        Color.White));
                    messages.Enqueue(new ColoredMessage($"Faulting endpoint:succeeded {faultingRequestsSucceeded:00}, ",
                        Color.Green));
                    messages.Enqueue(new ColoredMessage(
                        $"Faulting endpoint:pending {faultingRequestsMade - faultingRequestsSucceeded - faultingRequestsFailed:00}, ",
                        Color.Yellow));
                    messages.Enqueue(new ColoredMessage($"Faulting endpoint:failed {faultingRequestsFailed:00}.",
                        Color.Red));
                    messages.Enqueue(new ColoredMessage(string.Empty));

                    // Output all messages available right now, in one go.
                    progress.Report(ProgressWithMessages(ConsumeAsEnumerable(messages)));

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

            sharedBulkhead.Dispose();
        }

        public static IEnumerable<T> ConsumeAsEnumerable<T>(ConcurrentQueue<T> concurrentQueue)
        {
            while (concurrentQueue.TryDequeue(out var got)) yield return got;
        }

        public override Statistic[] LatestStatistics => new[]
        {
            new Statistic("Total requests made", totalRequests, Color.Default),
            new Statistic("Good endpoint: requested", goodRequestsMade, Color.Default),
            new Statistic("Good endpoint: succeeded", goodRequestsSucceeded, Color.Green),
            new Statistic("Good endpoint: pending", goodRequestsMade - goodRequestsSucceeded - goodRequestsFailed,
                Color.Yellow),
            new Statistic("Good endpoint: failed", goodRequestsFailed, Color.Red),
            new Statistic("Faulting endpoint: requested", faultingRequestsMade, Color.Default),
            new Statistic("Faulting endpoint: succeeded", faultingRequestsSucceeded, Color.Green),
            new Statistic("Faulting endpoint: pending",
                faultingRequestsMade - faultingRequestsSucceeded - faultingRequestsFailed, Color.Yellow),
            new Statistic("Faulting endpoint: failed", faultingRequestsFailed, Color.Red),
        };
    }
}