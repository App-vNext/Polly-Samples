namespace PollyTestClientConsole.Menu;

public record ConsoleMenuItem(string Name, Action Handler)
{
    public override string ToString() => Name;
}
