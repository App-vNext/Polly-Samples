using PollyDemos.OutputHelpers;

namespace PollyDemos
{
    public abstract class DemoBase
    {
        protected int TotalRequests;
        protected int EventualSuccesses;
        protected int EventualFailures;
        protected int Retries;

        // In the case of WPF the stdIn is redirected.
        protected static bool ShouldTerminateByKeyPress() => !Console.IsInputRedirected && Console.KeyAvailable;

        public virtual string Description => $"[Description for demo {GetType().Name} not yet provided.]";

        public abstract Statistic[] LatestStatistics { get; }

        public DemoProgress ProgressWithMessage(string message)
            => new(LatestStatistics, new ColoredMessage(message));

        public DemoProgress ProgressWithMessage(string message, Color color)
            => new(LatestStatistics, new ColoredMessage(message, color));

        public DemoProgress ProgressWithMessages(IEnumerable<ColoredMessage> messages)
            => new (LatestStatistics, messages);

        public void PrintHeader(IProgress<DemoProgress> progress)
        {
            progress.Report(ProgressWithMessage(GetType().Name));
            progress.Report(ProgressWithMessage("======"));
            progress.Report(ProgressWithMessage(string.Empty));
        }
    }
}
