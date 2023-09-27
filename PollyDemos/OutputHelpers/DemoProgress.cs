namespace PollyDemos.OutputHelpers
{
    public record struct DemoProgress(Statistic[] Statistics, IEnumerable<ColoredMessage> Messages)
    {
        public DemoProgress(Statistic[] statistics, ColoredMessage message)
            : this(statistics, new[] { message })
        {
        }
    }
}
