
using System.Diagnostics;
using Polly.Timeout;

namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates using a Retry, a Timeout and two Fallback strategies.
    /// In this demo, the delay in the retry is deliberately so long that the timeout wrapping it will time it out
    /// (in lieu for now of a demo server endpoint responding slowly).
    ///
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations:
    /// - though the console logs that a retry will be made, the 4-second wait before the retry is pre-emptively timed-out by the two-second timeout
    /// - a fallback strategy then provides substitute message for the user
    /// - otherwise similar to demo08.
    /// </summary>
    public static class Demo09_Pipeline_Fallback_Timeout_WaitAndRetry
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo09_Pipeline_Fallback_Timeout_WaitAndRetry));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailuresDueToTimeout = 0;
            int eventualFailuresForOtherReasons = 0;
            int totalRequests = 0;
            Stopwatch? watch = null;

            var pipelineBuilder = new ResiliencePipelineBuilder<string>();

            // Define our timeout strategy: time out after 2 seconds.
            pipelineBuilder.AddTimeout(new TimeoutStrategyOptions()
            {
                Timeout = TimeSpan.FromSeconds(2),
                OnTimeout = args =>
                {
                    var logMessage = $".The task was terminated because it ran out of time. Time cap was {args.Timeout.TotalSeconds}s";
                    ConsoleHelper.WriteLineInColor(logMessage, ConsoleColor.Yellow);
                    return default;
                }
            });

            // Define our retry strategy: keep retrying with 4 second gaps. This is (intentionally) too long: to demonstrate that the timeout strategy will time out on this before waiting for the retry.
            pipelineBuilder.AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<Exception>(),
                Delay = TimeSpan.FromSeconds(4),
                MaxRetryAttempts = int.MaxValue,
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    ConsoleHelper.WriteLineInColor($".Log,then retry: {exception.Message}", ConsoleColor.Yellow);
                    retries++;
                    return default;
                }
            });

            // Define a fallback strategy: provide a substitute message to the user, if we found the call was rejected due to timeout.
            pipelineBuilder.AddFallback(new()
            {
                ShouldHandle = new PredicateBuilder<string>().Handle<TimeoutRejectedException>(),
                FallbackAction = args => Outcome.FromResultAsValueTask("Please try again later [Fallback for timeout]"),
                OnFallback = args =>
                {
                    watch!.Stop();
                    var exception = args.Outcome.Exception!;
                    ConsoleHelper.WriteLineInColor($"Fallback catches failed with: {exception.Message} (after {watch.ElapsedMilliseconds}ms)", ConsoleColor.Red);
                    eventualFailuresDueToTimeout++;
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
            // Timeout
            // Retry
            // Fallback for timeout
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
            Console.WriteLine($"Requests timed out by timeout strategy  : {eventualFailuresDueToTimeout}");
            Console.WriteLine($"Requests which failed after longer delay: {eventualFailuresForOtherReasons}");
        }
    }
}
