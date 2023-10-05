namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates the Retry strategy with delays between retry attempts.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations: We now have waits and enough retries: all calls now succeed!  Yay!
    /// But we kind-a had to guess how many retries would be enough before the server responded again ...
    /// (and we're hammering that server with retries)
    /// </summary>
    public static class Demo03_WaitAndRetryNTimes_WithEnoughRetries
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo03_WaitAndRetryNTimes_WithEnoughRetries));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailures = 0;
            int totalRequests = 0;

            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 20, // Retry up to 20 times - this should be enough that we eventually succeed.
                Delay = TimeSpan.FromMilliseconds(200),
                OnRetry = args =>
                {
                    var exception = args.Outcome.Exception!;
                    ConsoleHelper.WriteLineInColor($"Strategy logging: {exception.Message}", ConsoleColor.Yellow);
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
