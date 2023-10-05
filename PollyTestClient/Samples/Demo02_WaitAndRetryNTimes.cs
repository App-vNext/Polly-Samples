namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates the Retry strategy coming into action.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations: We now have waits among the retries.
    /// In this case, still not enough wait - or not enough retries - for the underlying system to have recovered.
    /// So we still fail some calls.
    /// </summary>
    public static class Demo02_WaitAndRetryNTimes
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo02_WaitAndRetryNTimes));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailures = 0;
            int totalRequests = 0;

            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200), // Wait between each try
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
