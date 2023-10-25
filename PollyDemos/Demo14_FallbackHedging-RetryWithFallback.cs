using System.Net;
using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Imagine a microservice with an endpoint of varying response status codes for the same requests.<br/>
///     Most of the time it responds with success, but other times it sends a failure response.
/// </para>
/// <para>
///     If we can assume that this is just a transient failure then retries could help.<br/>
///     Hedging can be used to issue the same request as a retry or craft a hedged request.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>Same as in the previous demo.</item>
///         <item>But this time hedging will act as a combined retry and fallback strategy.</item>
///         <item><When hedging runs out of attempts, it returns a static fallback response.</item>
///     </list>
/// </para>
/// Take a look at the logs for PollyTestWebApi's requests to see the duplicates.
/// </summary>
public class Demo14_FallbackHedging_RetryWithFallback : DemoBase
{
    private const int MaxRetries = 2;
    private readonly ResiliencePropertyKey<int> requestIdKey = new("RequestId");
    private readonly ResiliencePropertyKey<int> attemptNumberKey = new("AttemptNumber");
    public override string Description =>
        "Demonstrates a mitigation action for failed responses. If the response indicates failure then it will issue a new request. The hedging strategy will either return a success response or a fallback.";

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        EventualSuccesses = 0;
        Retries = 0;
        EventualFailures = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        var strategy = new ResiliencePipelineBuilder<HttpResponseMessage>().AddHedging(new()
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>().HandleResult(res => !res.IsSuccessStatusCode),
            MaxHedgedAttempts = MaxRetries,
            Delay = TimeSpan.FromMilliseconds(-1),
            OnHedging = args =>
            {
                var requestId = $"{args.ActionContext.Properties.GetValue(requestIdKey, 0)}-{args.AttemptNumber}";

                var hedgedRequestNumber = args.AttemptNumber + 1;
                args.ActionContext.Properties.Set(attemptNumberKey, hedgedRequestNumber);

                progress.Report(ProgressWithMessage($"Strategy logging: Failed response for request #{requestId} detected. Preparing to execute the {hedgedRequestNumber} hedged action.", Color.Yellow));
                Retries++;
                return default;
            },
            // Alter the default behavior to perform retries until it runs out of attempts,
            // then it should return a fallback value
            ActionGenerator = args =>
            {
                if(args.AttemptNumber != MaxRetries)
                {
                    // Issue the original request again as the new hedged request
                    return () => args.Callback(args.ActionContext);
                }

                // Return a static fallback value
                var fallbackResponse = new HttpResponseMessage(HttpStatusCode.RequestTimeout) { Content = new StringContent("Fallback response was provided")};
                return () => Outcome.FromResultAsValueTask(fallbackResponse);
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

                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                progress.Report(ProgressWithMessage($"Response : {responseBody}", Color.Green));
                EventualSuccesses++;
            }
            catch (Exception e)
            {
                progress.Report(ProgressWithMessage($"Request {TotalRequests} eventually failed with: {e.Message}", Color.Red));
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
