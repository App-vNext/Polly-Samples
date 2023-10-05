using System.Diagnostics;
using Polly.CircuitBreaker;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates a  using a Retry, a CircuitBreaker and two Fallback strategies
    /// Same as Demo07 but now uses Fallback strategies to provide substitute values, when the call still fails overall.
    ///
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations:
    /// - operation is identical to Demo06 and Demo07
    /// - except fallback strategies provide nice substitute messages, if still fails overall
    /// - onFallback delegate captures the stats that were captured in try/catches in demos 06 and 07
    /// - also demonstrates how you can use the same kind of strategy (Fallback in this case) twice (or more) in a pipeline.
    /// </summary>
    public static class Demo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo08_Pipeline_Fallback_WaitAndRetry_CircuitBreaker));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToCircuitBreaking = 0;
            int eventualFailuresForOtherReasons = 0;
            int totalRequests = 0;
            Stopwatch? watch = null;

            // Provide the return type (string) to be able to use Fallback.
            var pipelineBuilder = new ResiliencePipelineBuilder<string>();

            pipelineBuilder.AddCircuitBreaker(new()
            {
                // Since pipeline has a string type parameter that's why the PredicateBuilder has to have one as well.
                ShouldHandle = new PredicateBuilder<string>().Handle<Exception>(),
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
                    ConsoleHelper.WriteLineInColor($"Strategy logging: {exception.Message}", ConsoleColor.Yellow);
                    retries++;
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
                    ConsoleHelper.WriteLineInColor($"Fallback catches failed with: {exception.Message} (after {watch.ElapsedMilliseconds}ms)", ConsoleColor.Red);
                    eventualFailuresDueToCircuitBreaking++;
                    return default;
                }
            });

            // Define a fallback strategy: provide a substitute message to the user, for any exception.
            pipelineBuilder.AddFallback(new()
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<Exception>(),
                FallbackAction = args => Outcome.FromResultAsValueTask("Please try again later [Fallback for any exception]"),
                OnFallback = args =>
                {
                    watch!.Stop();
                    var exception = args.Outcome.Exception!;
                    ConsoleHelper.WriteLineInColor($"Fallback catches eventually failed with: {exception.Message} (after {watch.ElapsedMilliseconds}ms)", ConsoleColor.Red);
                    eventualFailuresForOtherReasons++;
                    return default;
                }
            });

            // Build the pipeline which now composes four strategies (from inner to outer):
            // Circuit Breaker
            // Retry
            // Fallback for open circuit
            // Fallback for any other exception
            var pipeline = pipelineBuilder.Build();

            while (!Console.KeyAvailable)
            {
                totalRequests++;
                watch = Stopwatch.StartNew();

                try
                {
                    string responseBody = pipeline.Execute(token => HttpClientHelper.IssueRequestAndProcessResponse(totalRequests, token), CancellationToken.None);

                    watch.Stop();
                    ConsoleHelper.WriteInColor($"Response : {responseBody}", ConsoleColor.Green);
                    ConsoleHelper.WriteLineInColor($" (after {watch.ElapsedMilliseconds}ms)", ConsoleColor.Green);
                    eventualSuccesses++;
                }
                // This try-catch is not needed, since we have a Fallback for any Exceptions.
                // It's only been left in to *demonstrate* it should never get hit.
                catch (Exception e)
                {
                    var errorMessage = "Should never arrive here. Use of fallback for any Exception should have provided nice fallback value for exceptions.";
                    throw new UnreachableException(errorMessage, e);
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
