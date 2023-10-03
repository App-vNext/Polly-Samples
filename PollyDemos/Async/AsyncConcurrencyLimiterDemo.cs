namespace PollyDemos.Async
{
    public abstract class AsyncConcurrencyLimiterDemo : AsyncDemo
    {
        // Track the number of 'good' and 'faulting' requests made, succeeded and failed.
        // At any time, requests pending = made - succeeded - failed.
        protected int GoodRequestsMade;
        protected int GoodRequestsSucceeded;
        protected int GoodRequestsFailed;
        protected int FaultingRequestsMade;
        protected int FaultingRequestsSucceeded;
        protected int FaultingRequestsFailed;

        protected async Task<string> IssueFaultingRequestAndProcessResponseAsync(HttpClient client, CancellationToken cancellationToken)
            => await IssueRequestAndProcessResponseAsync(client, "nonthrottledfaulting", cancellationToken);

        protected async Task<string> IssueGoodRequestAndProcessResponseAsync(HttpClient client, CancellationToken cancellationToken)
            => await IssueRequestAndProcessResponseAsync(client, "nonthrottledgood", cancellationToken);

        private async Task<string> IssueRequestAndProcessResponseAsync(HttpClient client, string route, CancellationToken cancellationToken)
        {
            var response = await client
                .GetAsync($"{Configuration.WEB_API_ROOT}/api/{route}/{TotalRequests}", cancellationToken)
                .ConfigureAwait(false);
            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            return responseBody;
        }
    }
}
