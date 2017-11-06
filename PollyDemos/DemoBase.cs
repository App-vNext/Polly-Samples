using System;
using System.Collections.Generic;
using System.Configuration;
using PollyDemos.OutputHelpers;

namespace PollyDemos
{
    public abstract class DemoBase
    {
        protected bool TerminateDemosByKeyPress { get; } = ( ConfigurationManager.AppSettings["TerminateDemosByKeyPress"] ?? String.Empty).Equals(Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase);

        public virtual string Description
        {
            get { return $"[Description for demo {GetType().Name} not yet provided.]"; }
        }

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
