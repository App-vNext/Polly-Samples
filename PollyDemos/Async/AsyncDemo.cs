using PollyDemos.OutputHelpers;

namespace PollyDemos.Async
{
    public abstract class AsyncDemo : DemoBase
    {
        public abstract Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress);

        public async Task<string> IssueRequestAndProcessResponseAsync(HttpClient client, CancellationToken cancellationToken)
            => await client.GetStringAsync($"{Configuration.WEB_API_ROOT}/api/values/{TotalRequests}", cancellationToken);
    }
}
