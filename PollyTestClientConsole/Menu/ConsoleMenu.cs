using Spectre.Console;

namespace PollyTestClientConsole.Menu;

public static class ConsoleMenu
{
    private static readonly List<string> PollyAsciiArt =
    [
        "                                                                                ",
        "                                                                                ",
        "                                                               .,,,*******,     ",
        "                                                          .,,,,,,,,*********,   ",
        "                                            (((####%%,   ..,,,,,,,,***********  ",
        "                   ((,                    ((((((            ,,,,,,,***********. ",
        "                   (((/                   ((((,             .,,,,,,***********/ ",
        "        ((         /((((                (,((((    */////*    ,,,,,,***********/ ",
        "       .#(((       //(((((             .(( (((   ./#@@#&*,   ,,*//         ,**  ",
        "       .##((((*    ///((((((.           ((( ((((   *&@@/.   ,,,    *.           ",
        "       .##(((((((  .///(((((((#,         ,(((   ###      ..,,,,,,,              ",
        "        (##((((((((( ///(((((#####/         .####%%%     #%,,,,,,,,,            ",
        "         ####(((((((((((((((############         %%%%%  (#,                     ",
        "         ,#####(((((((((((((((#############%%%%%%%%%%%%%%%                      ",
        "           ####%#(((((((((((((((((((#####%%%%%%%%%%##(%%%                       ",
        "  ((        .##%%%%#(((((((((((((((((((((((((((((((((#%%%                       ",
        "  .(((((.     ,%%%%%%%%%((((((((((((((((((((((((((((%%%%*                       ",
        "   /((((((((((*  %%%%%%%%%%%%%%%%#####%%#(((((((((#%%%%%                        ",
        "    ,(((((((((((((((((((((((((((((((((((((((((((#%%%%%(                         ",
        "      ((((((((((((((((((((((((((((((((((((((((%%%%%%%                           ",
        "       .(((((#((((((((((((((((((((((((((((%%%%%%%%%                             ",
        "          (#########((((((((((((((((#%%%%%%%%%%%%                               ",
        "             /#############%%%%%%%%%%%%%%%%%%%                                  ",
        "                 .########%%%%%%%%%%%%%%%*                                      ",
        "                           .%%%*  %%%%%(                                        ",
        "            .%%%%%%%%*(((((((( %%%%%%%%(  *********                             ",
        "            .%%%%%%%%#(((((((((%%%%%%%%(  *********                             ",
        "            .%%%%%%%%#((((*((((%%%%%%%%(  *********                             ",
        "            .%%%%    *((((*((((%%%%%%%%(  *********                             ",
        "            .%%%%    *(((((((( %%%%%%%%%%%*********                             ",
        "                               %%%%%%%%%%%*********                             ",
        "                                                                                ",
    ];

    public static void PrintSplashScreen()
    {
        Console.Clear();
        Console.WriteLine("Welcome to Polly Demos!");

        foreach(var line in PollyAsciiArt)
        {
            Console.WriteLine(line);
        }

        Thread.Sleep(TimeSpan.FromSeconds(2.5));
    }

    public static void Run(List<ConsoleMenuItem> items)
    {
        while (true)
        {
            Console.Clear();

            var demo = AnsiConsole.Prompt(
                new SelectionPrompt<ConsoleMenuItem>()
                    .Title("Select a demo to run.")
                    .PageSize(items.Count)
                    .AddChoices(items));

            Console.WriteLine("Press Escape to return to menu.");

            demo.Handler();

            do
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
            }
            while (Console.ReadKey() is not { Key: ConsoleKey.Escape });
        }
    }
}
