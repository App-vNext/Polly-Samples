using System.Diagnostics;
using Polly.CircuitBreaker;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates using the Retry and CircuitBreaker strategies.
    /// Same as Demo06 but this time combines the strategies by using ResiliencePipelineBuilder.
    ///
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations:
    /// The operation is identical to Demo06.
    /// The code demonstrates how using the ResiliencePipelineBuilder makes your combined pipeline more concise, at the point of execution.
    /// </summary>
    public static class Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo07_WaitAndRetryNestingCircuitBreakerUsingPipeline));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToCircuitBreaking = 0;
            int eventualFailuresForOtherReasons = 0;
            int totalRequests = 0;

            // Define a pipeline builder which will be used to compose strategies incrementally.
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // The order of strategy definitions has changed.
            // Circuit breaker comes first because that will be the inner strategy.
            // Retry comes second because that will be the outer strategy.

            pipelineBuilder.AddCircuitBreaker(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                FailureRatio = 1.0,
                MinimumThroughput = 4,
                BreakDuration = TimeSpan.FromSeconds(3),
                OnOpened = args =>
                {
                    ConsoleHelper.WriteLineInColor(
                            $".Breaker logging: Breaking the circuit for {args.BreakDuration.TotalMilliseconds}ms!",
                            ConsoleColor.Magenta);

                    var exception = args.Outcome.Exception!;
                    ConsoleHelper.WriteLineInColor($"..due to: {exception.Message}", ConsoleColor.Magenta);
                    return default;
                },
                OnClosed = args =>
                {
                    ConsoleHelper.WriteLineInColor(".Breaker logging: Call OK! Closed the circuit again!", ConsoleColor.Magenta);
                    return default;
                },
                OnHalfOpened = args =>
                {
                    ConsoleHelper.WriteLineInColor(".Breaker logging: Half-open: Next call is a trial!", ConsoleColor.Magenta);
                    return default;
                }
            }); // We are not calling the Build method because we want to add one more strategy to the pipeline.

            pipelineBuilder.AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not BrokenCircuitException),
                MaxRetryAttempts = int.MaxValue,
                Delay = TimeSpan.FromMilliseconds(200),
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    ConsoleHelper.WriteLineInColor($"Strategy logging: {exception.Message}", ConsoleColor.Yellow);
                    retries++;
                    return default;
                }
            }); // We are not calling the Build method here because we will do it as a separate step to make the code cleaner.

            // Build the pipeline since we have added all the necessary strategies to it.
            var pipeline = pipelineBuilder.Build();

            while (!Console.KeyAvailable)
            {
                totalRequests++;
                var watch = Stopwatch.StartNew();

                try
                {
                     // Manage the call according to the pipeline.
                    string responseBody = pipeline.Execute(token =>
                    {
                        // This code is executed through both strategies in the pipeline:
                        // Retry is the outer, and circuit breaker is the inner.
                        // Demo 06 shows a decomposed version of what this is equivalent to.

                        return HttpClientHelper.IssueRequestAndProcessResponse(totalRequests, token);
                    }, CancellationToken.None);

                    watch.Stop();
                    ConsoleHelper.WriteInColor($"Response : {responseBody}", ConsoleColor.Green);
                    ConsoleHelper.WriteLineInColor($" (after {watch.ElapsedMilliseconds}ms)", ConsoleColor.Green);

                    eventualSuccesses++;
                }
                catch (BrokenCircuitException bce)
                {
                    watch.Stop();
                    ConsoleHelper.WriteInColor($"Request {totalRequests} failed with: {bce.GetType().Name}", ConsoleColor.Red);
                    ConsoleHelper.WriteLineInColor($" (after {watch.ElapsedMilliseconds}ms)", ConsoleColor.Red);
                    eventualFailuresDueToCircuitBreaking++;
                }
                catch (Exception e)
                {
                    watch.Stop();
                    ConsoleHelper.WriteInColor($"Request {totalRequests} eventually failed with: {e.Message}", ConsoleColor.Red);
                    ConsoleHelper.WriteLineInColor($" (after {watch.ElapsedMilliseconds}ms)", ConsoleColor.Red);
                    eventualFailuresForOtherReasons++;
                }

                Thread.Sleep(500);
            }

            Console.WriteLine();
            Console.WriteLine($"Total requests made                     : {totalRequests}");
            Console.WriteLine($"Requests which eventually succeeded     : {eventualSuccesses}");
            Console.WriteLine($"Retries made to help achieve success    : {retries}");
            Console.WriteLine($"Requests failed early by broken circuit : {eventualFailuresDueToCircuitBreaking}");
            Console.WriteLine($"Requests which failed after longer delay: {eventualFailuresForOtherReasons}");
        }
    }
}
