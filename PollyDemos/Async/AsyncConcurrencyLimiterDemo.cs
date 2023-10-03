namespace PollyDemos.Async
{
    public abstract class AsyncConcurrencyLimiterDemo : AsyncDemo
    {
        // Track the number of 'good' and 'faulting' requests made, succeeded and failed.
        // At any time, requests pending = made - succeeded - failed.
        protected int goodRequestsMade;
        protected int goodRequestsSucceeded;
        protected int goodRequestsFailed;
        protected int faultingRequestsMade;
        protected int faultingRequestsSucceeded;
        protected int faultingRequestsFailed;

        protected async Task<string> IssueFaultingRequestAndProcessResponseAsync(HttpClient client, CancellationToken cancellationToken)
            => await IssueRequestAndProcessResponseAsync(client, "nonthrottledfaulting", cancellationToken);

        protected async Task<string> IssueGoodRequestAndProcessResponseAsync(HttpClient client, CancellationToken cancellationToken)
            => await IssueRequestAndProcessResponseAsync(client, "nonthrottledgood", cancellationToken);

        private async Task<string> IssueRequestAndProcessResponseAsync(HttpClient client, string route, CancellationToken cancellationToken)
        {
            var response = await client
                .GetAsync($"{Configuration.WEB_API_ROOT}/api/{route}/{totalRequests}", cancellationToken)
                .ConfigureAwait(false);
            var responseBody = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);
            return responseBody;
        }
    }
}
