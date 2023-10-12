using PollyDemos.OutputHelpers;

namespace PollyTestClientConsole;

public static class ConsoleColorHelper
{
    public static ConsoleColor ToConsoleColor(this Color color) => color switch
    {
        Color.White or Color.Default => ConsoleColor.White,
        Color.Green => ConsoleColor.Green,
        Color.Magenta => ConsoleColor.Magenta,
        Color.Red => ConsoleColor.Red,
        Color.Yellow => ConsoleColor.Yellow,
        _ => throw new ArgumentOutOfRangeException(nameof(color)),
    };
}
