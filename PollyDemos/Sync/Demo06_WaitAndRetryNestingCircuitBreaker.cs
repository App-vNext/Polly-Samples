using System.Diagnostics;
using Polly.CircuitBreaker;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    /// <summary>
    /// Demonstrates using the Retry strategy nesting CircuitBreaker.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Discussion:  What if the underlying system was completely down?
    /// Keeping retrying would be pointless...
    /// ... and would leave the client hanging, retrying for successes which never come.
    ///
    /// Enter circuit-breaker:
    /// After too many failures, breaks the circuit for a period, during which it blocks calls + fails fast.
    /// - protects the downstream system from too many calls if it's really struggling (reduces load, so it can recover)
    /// - allows the client to get a fail response fast, not wait for ages, if downstream is AWOL.
    ///
    /// Observations:
    /// Note how after the circuit decides to break, subsequent calls fail faster.
    /// Note how breaker gives underlying system time to recover ...
    /// ... by the time circuit closes again, underlying system has recovered!
    /// </summary>
    public class Demo06_WaitAndRetryNestingCircuitBreaker : SyncDemo
    {
        private int eventualFailuresDueToCircuitBreaking;
        private int eventualFailuresForOtherReasons;

        public override string Description =>
            "This demonstrates CircuitBreaker. When an underlying system is completely down or seriously struggling, it can be better to fail fast and not put calls through.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            // Let's call a web API service to make repeated requests to a server.
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToCircuitBreaking = 0;
            eventualFailuresForOtherReasons = 0;
            totalRequests = 0;

            PrintHeader(progress, nameof(Demo06_WaitAndRetryNestingCircuitBreaker));

            // Define our retry strategy:
            var retryStrategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                // Exception filtering - we don't retry if the inner circuit-breaker judges the underlying system is out of commission.
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not BrokenCircuitException),
                MaxRetryAttempts = int.MaxValue, // Retry indefinitely
                Delay = TimeSpan.FromMilliseconds(200),  // Wait 200ms between each try
                OnRetry = args =>
                {
                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; // The Exception property is nullable

                    // Tell the user what happened
                    progress.Report(ProgressWithMessage($"Strategy logging: {exception.Message}", Color.Yellow));
                    retries++;
                    return default;
                }
            }).Build();

            // Define our circuit breaker strategy: break if the action fails 4 times in a row.
            var circuitBreakerStrategy = new ResiliencePipelineBuilder().AddCircuitBreaker(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                FailureRatio = 1.0,
                MinimumThroughput = 4,
                BreakDuration = TimeSpan.FromSeconds(3),
                OnOpened = args =>
                {
                    progress.Report(ProgressWithMessage(
                            $".Breaker logging: Breaking the circuit for {args.BreakDuration.TotalMilliseconds}ms!",
                            Color.Magenta));

                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; // The Exception property is nullable
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
            }).Build();

            var client = new HttpClient();
            var internalCancel = false;

            // Do the following until a key is pressed
            while (!(internalCancel || cancellationToken.IsCancellationRequested))
            {
                totalRequests++;
                var watch = Stopwatch.StartNew();

                try
                {
                    // Retry the following call according to the strategy.
                    retryStrategy.Execute(outerToken =>
                    {
                        // This code is executed within the retry strategy.

                        // Note how we can also Execute() a Func<TResult> and pass back the value.
                        var responseBody = circuitBreakerStrategy.Execute(innerToken =>
                        {
                            // This code is executed within the circuit breaker strategy.

                            return IssueRequestAndProcessResponse(client, innerToken);

                        }, outerToken);

                        watch.Stop();

                        // Display the response message on the console
                        progress.Report(ProgressWithMessage($"Response : {responseBody} (after {watch.ElapsedMilliseconds}ms)", Color.Green));
                        eventualSuccesses++;

                    }, cancellationToken);
                }
                catch (BrokenCircuitException bce)
                {
                    watch.Stop();
                    var logMessage = $"Request {totalRequests} failed with: {bce.GetType().Name} (after {watch.ElapsedMilliseconds}ms)";
                    progress.Report(ProgressWithMessage(logMessage,Color.Red));
                    eventualFailuresDueToCircuitBreaking++;
                }
                catch (Exception e)
                {
                    watch.Stop();
                    var logMessage = $"Request {totalRequests} eventually failed with: {e.Message} (after {watch.ElapsedMilliseconds}ms)";
                    progress.Report(ProgressWithMessage(logMessage,Color.Red));
                    eventualFailuresForOtherReasons++;
                }

                Thread.Sleep(500);
                internalCancel = TerminateDemosByKeyPress && Console.KeyAvailable;
            }
        }

        public override Statistic[] LatestStatistics => new Statistic[]
        {
            new("Total requests made", totalRequests),
            new("Requests which eventually succeeded", eventualSuccesses, Color.Green),
            new("Retries made to help achieve success", retries, Color.Yellow),
            new("Requests failed early by broken circuit", eventualFailuresDueToCircuitBreaking, Color.Magenta),
            new("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
        };
    }
}
