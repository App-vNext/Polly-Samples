using PollyDemos.OutputHelpers;

namespace PollyDemos
{
    public abstract class DemoBase
    {
        protected bool TerminateDemosByKeyPress { get; } = true;

        public virtual string Description => $"[Description for demo {GetType().Name} not yet provided.]";

        public abstract Statistic[] LatestStatistics { get; }

        public DemoProgress ProgressWithMessage(string message)
            => new(LatestStatistics, new ColoredMessage(message));

        public DemoProgress ProgressWithMessage(string message, Color color)
            => new(LatestStatistics, new ColoredMessage(message, color));

        public DemoProgress ProgressWithMessages(IEnumerable<ColoredMessage> messages)
            => new (LatestStatistics, messages);
    }
}
