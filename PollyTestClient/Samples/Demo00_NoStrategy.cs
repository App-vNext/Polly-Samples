namespace PollyTestClient.Samples
{
    /// <summary>
    /// Uses no policy. Demonstrates behavior of 'faulting server' we are testing against.
    /// Loops through a series of HTTP requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// </summary>
    public static class Demo00_NoStrategy
    {
        public static void Execute()
        {
            Console.WriteLine(nameof(Demo00_NoStrategy));
            Console.WriteLine("=======");

            int eventualSuccesses = 0;
            int retries = 0;
            int eventualFailures = 0;
            int totalRequests = 0;

            // Do the following until a key is pressed
            while (!Console.KeyAvailable)
            {
                totalRequests++;

                try
                {
                    var responseBody = HttpClientHelper.IssueRequestAndProcessResponse(totalRequests);
                    ConsoleHelper.WriteLineInColor($"Response : {responseBody}", ConsoleColor.Green);
                    eventualSuccesses++;
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
