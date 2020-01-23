using System;

namespace PollyDemos.OutputHelpers
{
    public struct ColoredMessage
    {
        public Color Color { get; }
        public string Message { get; }

        public ColoredMessage(string message, Color color)
        {
            Message = message;
            Color = color;
        }

        public ColoredMessage(string message) : this(message, Color.Default)
        {
        }
    }
}