using System.Diagnostics;
using Polly.CircuitBreaker;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    /// <summary>
    /// Demonstrates using the Retry and CircuitBreaker strategies.
    /// Same as Demo06 - but this time combines the strategies by using ResiliencePipelineBuilder.
    ///
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations:
    /// The operation is identical to Demo06.
    /// The code demonstrates how using the ResiliencePipelineBuilder makes your combined pipeline more concise, at the point of execution.
    /// </summary>
    public class Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToCircuitBreaking;
        private int eventualFailuresForOtherReasons;

        public override string Description =>
            "This demonstrates CircuitBreaker (see Demo06), but uses the ResiliencePipelineBuilder to compose the strategies. Only the underlying code differs.";

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

            progress.Report(ProgressWithMessage(nameof(Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            // New for demo07: here we define a pipeline builder which will be used to compose strategies gradually.
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // New for demo07: The order of strategy definitions has changed.
            // Circuit breaker comes first because that will be the inner strategy.
            // Retry comes second because that will be the outer strategy.

            // Define our circuit breaker strategy:
            pipelineBuilder.AddCircuitBreaker(new()
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
                    var exception = args.Outcome.Exception!; //The Exception property is nullable
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
            }); // New for demo07: here we are not calling the Build method because we want to add one more strategy to the pipeline.

            // Define our retry strategy:
            pipelineBuilder.AddRetry(new()
            {
                // Exception filtering - we don't retry if the inner circuit-breaker judges the underlying system is out of commission.
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not BrokenCircuitException),
                MaxRetryAttempts = int.MaxValue, // Retry indefinitely
                Delay = TimeSpan.FromMilliseconds(200),  // Wait 200ms between each try
                OnRetry = args =>
                {
                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; //The Exception property is nullable

                    // Tell the user what happened
                    progress.Report(ProgressWithMessage($"Strategy logging: {exception.Message}", Color.Yellow));
                    retries++;
                    return default;
                }
            }); // New for demo07: here we are not calling the Build method because we will do it as a separate step to make the code cleaner.

            // New for demo07: here we build the pipeline since we have added all the necessary strategies to it.
            var pipeline = pipelineBuilder.Build();

            var client = new HttpClient();
            var internalCancel = false;
            // Do the following until a key is pressed
            while (!(internalCancel || cancellationToken.IsCancellationRequested))
            {
                totalRequests++;
                var watch = Stopwatch.StartNew();

                try
                {
                    // Retry the following call according to the pipeline
                    var response = pipeline.Execute(ct =>
                    {
                        // This code is executed through both strategies in the pipeline:
                        // Retry is the outer, and circuit breaker is the inner.
                        // Demo 06 shows a broken-out version of what this is equivalent to.

                        // Make a request and get a response
                        var url = $"{Configuration.WEB_API_ROOT}/api/values/{totalRequests}";
                        var response = client.Send(new HttpRequestMessage(HttpMethod.Get, url), ct);

                        using var stream = response.Content.ReadAsStream(ct);
                        using var streamReader = new StreamReader(stream);
                        return streamReader.ReadToEnd();
                    }, cancellationToken);

                    watch.Stop();

                    // Display the response message on the console
                    progress.Report(ProgressWithMessage($"Response : {response} (after {watch.ElapsedMilliseconds}ms)", Color.Green));
                    eventualSuccesses++;
                }
                catch (BrokenCircuitException bce)
                {
                    watch.Stop();
                    var logMessage = $"Request {totalRequests} failed with: {bce.GetType().Name} (after {watch.ElapsedMilliseconds}ms)";
                    progress.Report(ProgressWithMessage(logMessage, Color.Red));
                    eventualFailuresDueToCircuitBreaking++;
                }
                catch (Exception e)
                {
                    watch.Stop();
                    var logMessage = $"Request {totalRequests} eventually failed with: {e.Message} (after {watch.ElapsedMilliseconds}ms)";
                    progress.Report(ProgressWithMessage(logMessage, Color.Red));
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
