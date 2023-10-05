namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates the Retry strategy with delays between retry attempts.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations: We no longer have to guess how many retries are enough.
    /// All calls still succeed!  Yay!
    /// But we're still hammering that underlying server with retries.
    /// Imagine if lots of clients were doing that simultaneously
    ///  - could just increase load on an already-struggling server!
    /// </summary>
    public static class Demo04_WaitAndRetryForever
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo04_WaitAndRetryForever));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailures = 0;
            int totalRequests = 0;

            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = int.MaxValue, // Retry indefinitely
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
