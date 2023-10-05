namespace PollyTestClient.Samples
{
    /// <summary>
    /// Demonstrates the Retry strategy coming into action.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    ///
    /// Observations: There's no wait among these retries. It can be appropriate sometimes.
    /// In this case, no wait hasn't given underlying system time to recover, so calls still fail despite retries.
    /// </summary>
    public static class Demo01_RetryNTimes
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo01_RetryNTimes));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailures = 0;
            int totalRequests = 0;

            // Define our strategy:
            var strategy = new ResiliencePipelineBuilder().AddRetry(new()
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = 3, // Retry up to 3 times
                OnRetry = args =>
                {
                    // Due to how we have defined ShouldHandle, this delegate is called only if an exception occurred.
                    // Note the ! sign (null-forgiving operator) at the end of the command.
                    var exception = args.Outcome.Exception!; // The Exception property is nullable

                    // Tell the user what happened
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
                    // Retry the following call according to the strategy.
                    strategy.Execute(token =>
                    {
                        // This code is executed within the strategy

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
