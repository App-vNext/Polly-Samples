using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Imagine a microservice with an endpoint of varying response status codes for the same requests.<br/>
///     Most of the time it responds with success, but other times it sends a failure response.
/// </para>
/// <para>
///     If we can assume that this is just a transient failure then retry could help.<br/>
///     Hedging can be used in a fallback mode to issue hedged requests in case of failure.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>When the original response indicates failure then a new hedged request is issued.</item>
///         <item>In this demo the hedging will act as a simple retry strategy.</item>
///         <item>In the next demo hedging will act as a combined retry and fallback strategy.</item>
///     </list>
/// </para>
/// Take a look at the logs for PollyTestWebApi's requests to see the duplicates.
/// </summary>
public class Demo13_FallbackHedging_RetryOnly : DemoBase
{
    private readonly ResiliencePropertyKey<int> requestIdKey = new("RequestId");
    private readonly ResiliencePropertyKey<int> attemptNumberKey = new("AttemptNumber");
    public override string Description =>
        "Demonstrates a mitigation action for failed responses. If the response indicates failure then it will issue a new request. The hedging strategy waits for the first successful response or it runs out of retry attempts.";

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        EventualSuccesses = 0;
        Retries = 0;
        EventualFailures = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        var strategy = new ResiliencePipelineBuilder<HttpResponseMessage>().AddHedging(new()
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(res => !res.IsSuccessStatusCode), // Handle unsuccessful responses
            MaxHedgedAttempts = 2, // Issue at most two extra hedged requests
            Delay = TimeSpan.FromMilliseconds(-1), // Issue a hedged request if response does not indicate success (fallback mode)
            OnHedging = args =>
            {
                var requestId = $"{args.ActionContext.Properties.GetValue(requestIdKey, 0)}-{args.AttemptNumber}";

                var hedgedRequestNumber = args.AttemptNumber + 1;
                args.ActionContext.Properties.Set(attemptNumberKey, hedgedRequestNumber);

                progress.Report(ProgressWithMessage($"Strategy logging: Failed response for request #{requestId} detected. Preparing to execute hedged action {hedgedRequestNumber}.", Color.Yellow));
                Retries++;
                return default;
            }
        }).Build();

        var client = new HttpClient();
        var internalCancel = false;

        while (!(internalCancel || cancellationToken.IsCancellationRequested))
        {
            TotalRequests++;

            ResilienceContext context = ResilienceContextPool.Shared.Get();

            try
            {
                context.Properties.Set(requestIdKey, TotalRequests);
                var response = await strategy.ExecuteAsync(async ctx =>
                {
                    var requestId = $"{TotalRequests}-{ctx.Properties.GetValue(attemptNumberKey, 0)}";
                    return await client.GetAsync($"{Configuration.WEB_API_ROOT}/api/VaryingResponseStatus/{requestId}", cancellationToken);
                },context);

                // If all requests failed then hedging will return the first request's response.
                // To handle that failure correctly, we call EnsureSuccessStatusCode to throw an HttpRequestException
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                progress.Report(ProgressWithMessage($"Response : {responseBody}", Color.Green));
                EventualSuccesses++;
            }
            catch (Exception e)
            {
                var requestId = $"{TotalRequests}-{context.Properties.GetValue(attemptNumberKey, 0)}";
                progress.Report(ProgressWithMessage($"Request {requestId} eventually failed with: {e.Message}", Color.Red));
                EventualFailures++;
            }
            finally
            {
                ResilienceContextPool.Shared.Return(context);
            }

            await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationToken);
            internalCancel = ShouldTerminateByKeyPress();
        }
    }

    public override Statistic[] LatestStatistics => new Statistic[]
    {
        new("Total requests made", TotalRequests),
        new("Requests which eventually succeeded", EventualSuccesses, Color.Green),
        new("Hedged action made to help achieve success", Retries, Color.Yellow),
        new("Requests which eventually failed", EventualFailures, Color.Red),
    };
}
