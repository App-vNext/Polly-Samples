using System.Diagnostics;
using Polly.Timeout;
using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
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
    public class Demo09_Pipeline_Fallback_Timeout_WaitAndRetry : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int retries;
        private int eventualFailuresDueToTimeout;
        private int eventualFailuresForOtherReasons;

        public override string Description =>
            "Demonstrates introducing a Timeout strategy. The timeout will eventually time-out on the retries. When we timeout, we again use a Fallback to substitute a more graceful message.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            ArgumentNullException.ThrowIfNull(progress);

            // Let's call a web API service to make repeated requests to a server.
            // The service is programmed to fail after 3 requests in 5 seconds.

            eventualSuccesses = 0;
            retries = 0;
            eventualFailuresDueToTimeout = 0;
            eventualFailuresForOtherReasons = 0;
            totalRequests = 0;

            progress.Report(ProgressWithMessage(nameof(Demo09_Pipeline_Fallback_Timeout_WaitAndRetry)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            Stopwatch? watch = null;
            var pipelineBuilder = new ResiliencePipelineBuilder<string>();

            // Define our timeout strategy: time out after 2 seconds.
            pipelineBuilder.AddTimeout(new TimeoutStrategyOptions()
            {
                Timeout = TimeSpan.FromSeconds(2),
                OnTimeout = args =>
                {
                    var logMessage = $".The task was terminated because it run out of time. Time cap was {args.Timeout.TotalSeconds}s";
                    progress.Report(ProgressWithMessage(logMessage, Color.Yellow));
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
                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; //The Exception property is nullable
                    progress.Report(ProgressWithMessage($".Log,then retry: {exception.Message}", Color.Yellow));
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

                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; //The Exception property is nullable

                    progress.Report(ProgressWithMessage($"Fallback catches failed with: {exception.Message} (after {watch.ElapsedMilliseconds}ms)", Color.Red));
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

                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; //The Exception property is nullable

                    progress.Report(ProgressWithMessage($"Fallback catches eventually failed with: {exception.Message} (after {watch.ElapsedMilliseconds}ms)", Color.Red));

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

            var client = new HttpClient();
            var internalCancel = false;
            // Do the following until a key is pressed
            while (!(internalCancel || cancellationToken.IsCancellationRequested))
            {
                totalRequests++;
                watch = Stopwatch.StartNew();

                try
                {
                    // Manage the call according to the pipeline.
                    var response = pipeline.Execute(ct =>
                    {
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
                // This try-catch is not needed, since we have a Fallback for any Exceptions.
                // It's only been left in to *demonstrate* it should never get hit.
                catch (Exception e)
                {
                    var errorMessage = "Should never arrive here. Use of fallback for any Exception should have provided nice fallback value for exceptions.";
                    throw new InvalidOperationException(errorMessage, e);
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
            new("Requests timed out by timeout strategy", eventualFailuresDueToTimeout, Color.Magenta),
            new("Requests which failed after longer delay", eventualFailuresForOtherReasons, Color.Red),
        };
    }
}
