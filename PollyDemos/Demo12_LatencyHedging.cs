using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// <para>
///     Imagine a microservice with an endpoint of varying response times.<br/>
///     Most of the time it responds in a timely manner, but sometimes it takes too long to send a response.
/// </para>
/// <para>
///     This problem is known as long tail latency. One of the well-known solutions for tail-tolerance is called hedged request.<br/>
///     A hedged request is issued (as a mitigation action) when the original request's response is considered too slow.<br/>
///     So, we have two pending requests: the original request and the hedged request.<br/>
///     The faster response will be propagated back to the caller. The slower one will receive the cancellation request signal.
/// </para>
/// <para>
///     Observations:
///     <list type="bullet">
///         <item>When the response arrives less than a second then the hedging will not be triggered.</item>
///         <item>When the response does not arrive on time then a second (hedged) request is issued as well.</item>
///         <item>Only the faster one is waited for (the other one is cancelled).</item>
///     </list>
/// </para>
/// <para>
///     How to read the demo logs:
///     <list type="bullet">
///         <item>"Deferred ... to request #N-0": The original request was faster.</item>
///         <item>"Deferred ... to request #N-1": The hedged request was faster.</item>
///     </list>
/// </para>
/// Take a look at the logs for PollyTestWebApi's requests to see the duplicates.
/// </summary>
public class Demo12_LatencyHedging : DemoBase
{
    // This demo also shows how to use resilience context.
    // We will set the request id just before we issue the original request.
    // We access this id inside the OnHedging delegate.
    // The resilience context is generic, so we need to use the ResiliencePropertyKey<int> type.
    private readonly ResiliencePropertyKey<int> requestIdKey = new("RequestId");

    // We will set the attempt number inside the OnHedging delegate.
    // We access this number inside the decorated callback.
    private readonly ResiliencePropertyKey<int> attemptNumberKey = new("AttemptNumber");

    public override string Description =>
        "Demonstrates a mitigation action for slow responses. If the response doesn't arrive within a second then it will issue a new request. The hedging strategy waits for the fastest response.";

    public override async Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress)
    {
        EventualSuccesses = 0;
        Retries = 0;
        EventualFailures = 0;
        TotalRequests = 0;

        PrintHeader(progress);

        var strategy = new ResiliencePipelineBuilder<HttpResponseMessage>().AddHedging(new()
        {
            MaxHedgedAttempts = 1, // Issue at most one extra hedged request
            Delay = TimeSpan.FromSeconds(1), // Wait one second before issuing the hedged request (latency mode)
            OnHedging = args =>
            {
                // Retrieve the request id from the context
                var requestId = $"{args.ActionContext.Properties.GetValue(requestIdKey, 0)}-{args.AttemptNumber}";

                // Set the attempt number on the context
                var hedgedRequestNumber = args.AttemptNumber + 1;
                args.ActionContext.Properties.Set(attemptNumberKey, hedgedRequestNumber);

                progress.Report(ProgressWithMessage($"Strategy logging: Slow response for request #{requestId} detected. Preparing to execute hedged action {hedgedRequestNumber}.", Color.Yellow));
                Retries++;
                return default;
            }
        }).Build();

        var client = new HttpClient();
        var internalCancel = false;

        while (!(internalCancel || cancellationToken.IsCancellationRequested))
        {
            TotalRequests++;

            // Retrieve a context from a context pool
            ResilienceContext context = ResilienceContextPool.Shared.Get();

            try
            {
                // Set the request id just before we issue the original request
                context.Properties.Set(requestIdKey, TotalRequests);
                var response = await strategy.ExecuteAsync(async ctx =>
                {
                    // Retrieve the attempt number from the context
                    var requestId = $"{TotalRequests}-{ctx.Properties.GetValue(attemptNumberKey, 0)}";
                    return await client.GetAsync($"{Configuration.WEB_API_ROOT}/api/VaryingResponseTime/{requestId}", cancellationToken);
                }, context);

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
                // Return the context to a context pool
                // It needs to be returned in case of success or failure that's why we used the finally block.
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
