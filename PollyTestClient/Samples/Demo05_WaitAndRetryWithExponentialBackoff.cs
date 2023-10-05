
namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates Retry strategy with calculated retry delays to back off.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations: All calls still succeed!  Yay!
    /// But we didn't hammer the underlying server so hard - we backed off.
    /// That's healthier for it, if it might be struggling ...
    /// ... and if a lot of clients might be doing this simultaneously.
    ///
    /// ... What if the underlying system was totally down tho?
    /// ... Keeping trying forever would be counterproductive (so, see Demo06)
    /// </summary>
    public static class Demo05_WaitAndRetryWithExponentialBackoff
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo05_WaitAndRetryWithExponentialBackoff));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailures = 0;
            int totalRequests = 0;

            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 6, // We could also retry indefinitely by using int.MaxValue
                BackoffType = DelayBackoffType.Exponential, // Back off: 1s, 2s, 4s, 8s, ... + jitter
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    ConsoleHelper.WriteLineInColor($"Exception: {exception.Message}", ConsoleColor.Yellow);
                    ConsoleHelper.WriteLineInColor($" ... automatically delaying for {args.RetryDelay.TotalMilliseconds}ms.", ConsoleColor.Yellow);
                    retries++;
                    return default;
                }
            }).Build();

            while (!Console.KeyAvailable)
            {
                totalRequests++;

                try
                {
                    strategy.Execute(token =>
                    {
                        var responseBody = HttpClientHelper.IssueRequestAndProcessResponse(totalRequests, token);
                        ConsoleHelper.WriteLineInColor($"Response : {responseBody}", ConsoleColor.Green);
                        eventualSuccesses++;
                    }, CancellationToken.None);
                }
                catch (Exception e)
                {
                    ConsoleHelper.WriteLineInColor($"Request {totalRequests} eventually failed with: {e.Message}", ConsoleColor.Red);
                    eventualFailures++;
                }

                Thread.Sleep(500);
            }

            Console.WriteLine();
            Console.WriteLine($"Total requests made                 : {totalRequests}");
            Console.WriteLine($"Requests which eventually succeeded : {eventualSuccesses}");
            Console.WriteLine($"Retries made to help achieve success: {retries}");
            Console.WriteLine($"Requests which eventually failed    : {eventualFailures}");
        }
    }
}
