using PollyDemos.OutputHelpers;

namespace PollyDemos.Helpers;

public abstract class DemoBase
{
    protected int TotalRequests;
    protected int EventualSuccesses;
    protected int EventualFailures;
    protected int Retries;

    // In the case of WPF the stdIn is redirected.
    protected static bool ShouldTerminateByKeyPress() => !Console.IsInputRedirected && Console.KeyAvailable;

    public abstract string Description {get;}

    public abstract Task ExecuteAsync(CancellationToken cancellationToken, IProgress<DemoProgress> progress);

    public abstract Statistic[] LatestStatistics { get; }

    protected DemoProgress ProgressWithMessage(string message)
        => new(LatestStatistics, new ColoredMessage(message));

    protected DemoProgress ProgressWithMessage(string message, Color color)
        => new(LatestStatistics, new ColoredMessage(message, color));

    protected void PrintHeader(IProgress<DemoProgress> progress)
    {
        progress.Report(ProgressWithMessage(GetType().Name));
        progress.Report(ProgressWithMessage("======"));
        progress.Report(ProgressWithMessage(string.Empty));
    }

    protected async Task<string> IssueRequestAndProcessResponseAsync(HttpClient client, CancellationToken cancellationToken)
        => await client.GetStringAsync($"{Configuration.WEB_API_ROOT}/api/values/{TotalRequests}", cancellationToken);
}
