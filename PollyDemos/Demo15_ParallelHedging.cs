using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Imagine a microservice with an endpoint of varying response status codes for the same requests.<br/>
///     Most of the time it responds with success, but other times it sends a failure response.
/// </para>
/// <para>
///     If we can assume that this is just a transient failure then parallel requests could help.<br/>
///     Hedging can be used in a parallel mode to issue all requests (original + hedged ones) simultaneously.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>Hedging initiates MaxHedgedAttempts + 1 requests concurrently.</item>
///         <item>The fastest non-failed response will be the final result.</item>
///         <item>If all attempts fail then the final result will be the original request's response.</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Success ... to request #N-0": The original request succeeded.</item>
///         <item>"Success ... to request #N-1": The first hedged request succeeded.</item>
///         <item>"Success ... to request #N-2": The last hedged request succeeded.</item>
///         <item>"Request batch N eventually failed with: ...": All requests failed.</item>
///     </list>
/// </para>
/// Take a look at the logs for PollyTestWebApi's requests to see the duplicates.
/// </summary>
public class Demo15_ParallelHedging : DemoBase
{
    // In this demo we issue requests in batches
    private int requestBatches = 0;
    private readonly ResiliencePropertyKey<int> requestIdKey = new("RequestId");
    private readonly ResiliencePropertyKey<int> attemptNumberKey = new("AttemptNumber");
    public override string Description =>
        "Demonstrates a prevention action for failed responses. The hedging strategy will initiate MaxRetryAttempt plus one requests simultaneously. The assumption is that at least one would succeeded, so there is no need for retry.";
    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        EventualSuccesses = 0;
        EventualFailures = 0;
        TotalRequests = 0;
        requestBatches = 0;

        PrintHeader(progress);

        var strategy = new ResiliencePipelineBuilder<HttpResponseMessage>().AddHedging(new()
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(res => !res.IsSuccessStatusCode),
            MaxHedgedAttempts = 2, // Issue the original request and 2 more hedged requests in parallel
            Delay = TimeSpan.Zero, // Parallel mode
            OnHedging = args =>
            {
                TotalRequests++; // Hedged request
                var hedgedRequestNumber = args.AttemptNumber + 1;
                var requestId = $"{args.ActionContext.Properties.GetValue(requestIdKey, 0)}-{hedgedRequestNumber}";
                args.ActionContext.Properties.Set(attemptNumberKey, hedgedRequestNumber);

                progress.Report(ProgressWithMessage($"Strategy logging: Preparing to execute hedged action {requestId}.", Color.Yellow));
                return default;
            },
        }).Build();

        var client = new HttpClient();
        var internalCancel = false;

        while (!(internalCancel || cancellationToken.IsCancellationRequested))
        {
            requestBatches++;
            TotalRequests++; // Original request

            ResilienceContext context = ResilienceContextPool.Shared.Get();

            try
            {
                context.Properties.Set(requestIdKey, requestBatches);
                var response = await strategy.ExecuteAsync(async ctx =>
                {
                    var requestId = $"{requestBatches}-{ctx.Properties.GetValue(attemptNumberKey, 0)}";
                    return await client.GetAsync($"{Configuration.WEB_API_ROOT}/api/VaryingResponseStatus/{requestId}?useJitter=true", cancellationToken);
                },context);


                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                progress.Report(ProgressWithMessage($"Response : {responseBody}", Color.Green));
                EventualSuccesses++;
            }
            catch (Exception e)
            {
                progress.Report(ProgressWithMessage($"Request batch {requestBatches} eventually failed with: {e.Message}", Color.Red));
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
        new("Request batches", requestBatches),
        new("Succeeded request batches", EventualSuccesses, Color.Green),
        new("Failed requests batches", EventualFailures, Color.Red),
    };
}
