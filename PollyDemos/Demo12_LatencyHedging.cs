using PollyDemos.Helpers;
using PollyDemos.OutputHelpers;

namespace PollyDemos;

/// <summary>
/// Imagine a microservice with an endpoint of varying response times.
/// Most of the times it responds in a timely manner, but sometimes it takes too long to send a response.
///
/// This problem is known as long tail latency. One of the well-known solutions for tail-tolerance is called hedged request.
/// A hedged request is issued (as a mitigation action) when the original request's response is considered too slow.
/// So, we have two pending requests: the original request and the hedged request.
/// The faster response will be propagated back to the caller. The slower will receive the cancellation request signal.
///
/// Observations:
/// When the response arrives less than a second then the hedging will not be triggered.
/// When the response did not arrive on time then a second (hedged) request is issued as well.
/// Only the faster one is awaited (the other is cancelled).
///
/// We suggest to access the PollyTestWebApi's requests log to see the duplicate requests.
/// </summary>
public class Demo12_LatencyHedging : DemoBase
{
    // This demo also shows how to use resilience context.
    // We will set the request id just before we issue the original request.
    // We access this id inside the OnHedging delegate.
    // The resilience context is type-safe that's why we need to use ResiliencePropertyKey<int>.
    private readonly ResiliencePropertyKey<int> requestIdKey = new("RequestId");

    public override string Description =>
        "Demonstrates a mitigation action for slow responses. If the response doesn't arrive less than a second then it will issue a new request. The hedging strategy waits for the fastest response.";

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
                var requestId = args.ActionContext.Properties.GetValue(requestIdKey, 0);
                progress.Report(ProgressWithMessage($"Strategy logging: Slow response for request #{requestId} detected. Preparing to execute the {args.AttemptNumber} hedged action.", Color.Yellow));
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
                var response = await strategy.ExecuteAsync(async _ =>
                    await client.GetAsync($"{Configuration.WEB_API_ROOT}/api/VaryingResponseTime/{TotalRequests}", cancellationToken),
                    context);

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
