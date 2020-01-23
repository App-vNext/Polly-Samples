using System.Collections.Generic;
using System.Linq;

namespace PollyDemos.OutputHelpers
{
    public struct DemoProgress
    {
        public Statistic[] Statistics { get; }
        public ColoredMessage[] Messages { get; }

        public DemoProgress(Statistic[] statistics, IEnumerable<ColoredMessage> messages)
        {
            Statistics = statistics;
            Messages = messages.ToArray();
        }

        public DemoProgress(Statistic[] statistics, ColoredMessage message)
        {
            Statistics = statistics;
            Messages = new[] {message};
        }
    }
}