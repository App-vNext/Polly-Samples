using PollyDemos.OutputHelpers;

namespace PollyDemos
{
    public abstract class DemoBase
    {
        protected bool TerminateDemosByKeyPress { get; } = true;

        public virtual string Description => $"[Description for demo {GetType().Name} not yet provided.]";

        public abstract Statistic[] LatestStatistics { get; }

        public DemoProgress ProgressWithMessage(string message)
        {
            return new DemoProgress(LatestStatistics, new ColoredMessage(message, Color.Default));
        }

        public DemoProgress ProgressWithMessage(string message, Color color)
        {
            return new DemoProgress(LatestStatistics, new ColoredMessage(message, color));
        }

        public DemoProgress ProgressWithMessages(IEnumerable<ColoredMessage> messages)
        {
            return new DemoProgress(LatestStatistics, messages);
        }
    }
}