using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Uses no strategy. <br/>
///     Demonstrates behavior of 'faulting server' we are testing against <br/>
///     Loops through a series of HTTP requests, keeping track of each requested <br/>
///     item and reporting server failures when encountering exceptions.
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Response: ... request #N(...)": Response received on time.</item>
///         <item>"Request N eventually failed ... (Too Many Requests)": Request is rejected due to rate limit exceeded.</item>
///     </list>
/// </para>
/// </summary>
public class Demo00_NoStrategy : DemoBase
{
    public override string Description =>
        "This demo demonstrates how our faulting server behaves, with no Polly strategy in use.";

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        EventualSuccesses = 0;
        EventualFailures = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        var client = new HttpClient();
        var internalCancel = false;

        // Do the following until a key is pressed
        while (!(internalCancel || cancellationToken.IsCancellationRequested))
        {
            TotalRequests++;

            try
            {
                // Make a request and get a response
                var responseBody = await IssueRequestAndProcessResponseAsync(client, cancellationToken);

                // Display the response message on the console
                progress.Report(ProgressWithMessage($"Response : {responseBody}", Color.Green));
                EventualSuccesses++;
            }
            catch (Exception e)
            {
                progress.Report(ProgressWithMessage($"Request {TotalRequests} eventually failed with: {e.Message}", Color.Red));
                EventualFailures++;
            }

            await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            internalCancel = ShouldTerminateByKeyPress();
        }
    }

    public override Statistic[] LatestStatistics => new Statistic[]
    {
        new("Total requests made", TotalRequests),
        new("Requests which eventually succeeded", EventualSuccesses, Color.Green),
        new("Retries made to help achieve success", Retries, Color.Yellow),
        new("Requests which eventually failed", EventualFailures, Color.Red),
    };
}
