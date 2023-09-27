using PollyDemos.OutputHelpers;

namespace PollyDemos.Sync
{
    /// <summary>
    /// Uses no policy.  Demonstrates behaviour of 'faulting server' we are testing against.
    /// Loops through a series of Http requests, keeping track of each requested
    /// item and reporting server failures when encountering exceptions.
    /// </summary>
    public class Demo00_NoPolicy : SyncDemo
    {
        private int totalRequests;
        private int eventualSuccesses;
        private int eventualFailures;

        public override string Description =>
            "This demo demonstrates how our faulting server behaves, with no Polly policies in use.";

        public override void Execute(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
        {
            if (progress is null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            // Let's call a web api service to make repeated requests to a server.
            // The service is programmed to fail after 3 requests in 5 seconds.
            progress.Report(ProgressWithMessage(nameof(Demo00_NoPolicy)));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));

            var client = new HttpClient();
            var internalCancel = false;
            // Do the following until a key is pressed
            while (!(internalCancel || cancellationToken.IsCancellationRequested))
            {
                totalRequests++;

                try
                {
                    // Make a request and get a response
                    var url = $"{Configuration.WEB_API_ROOT}/api/values/{totalRequests}";
                    var response = client.Send(new HttpRequestMessage(HttpMethod.Get, url));

                    // Display the response message on the console
                    using var stream = response.Content.ReadAsStream();
                    using var streamReader = new StreamReader(stream);
                    progress.Report(ProgressWithMessage($"Response : {streamReader.ReadToEnd()}", Color.Green));
                    eventualSuccesses++;
                }
                catch (Exception e)
                {
                    progress.Report(ProgressWithMessage($"Request {totalRequests} eventually failed with: {e.Message}", Color.Red));
                    eventualFailures++;
                }

                Thread.Sleep(500);
                internalCancel = TerminateDemosByKeyPress && Console.KeyAvailable;
            }
        }

        public override Statistic[] LatestStatistics => new Statistic[]
        {
            new("Total requests made", totalRequests),
            new("Requests which eventually succeeded", eventualSuccesses, Color.Green),
            new("Retries made to help achieve success", 0, Color.Yellow),
            new("Requests which eventually failed", eventualFailures, Color.Red),
        };
    }
}
