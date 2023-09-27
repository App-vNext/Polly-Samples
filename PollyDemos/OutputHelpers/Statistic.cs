namespace PollyDemos.OutputHelpers
{
    public record struct Statistic(string Description, double Value, Color Color)
    {
        public Statistic(string description, double value)
            : this(description, value, Color.Default)
        {
        }
    }
}
