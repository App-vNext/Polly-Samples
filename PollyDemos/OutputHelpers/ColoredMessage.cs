namespace PollyDemos.OutputHelpers;

public record struct ColoredMessage(string Message, Color Color)
{
    public ColoredMessage(string message)
        : this(message, Color.Default)
    {
    }
}
