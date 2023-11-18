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
        int index = 0;
        WriteMenu(items, items[index]);

        bool isRunning = true;
        while (isRunning)
        {
            Action nextAction = Console.ReadKey().Key switch
            {
                ConsoleKey.DownArrow when index + 1 < items.Count => () => WriteMenu(items, items[++index]),
                ConsoleKey.UpArrow when index - 1 >= 0 => () => WriteMenu(items, items[--index]),
                ConsoleKey.Enter => () =>
                {
                    Console.Clear();
                    items[index].Handler();
                    Console.ReadKey();
                    Console.WriteLine();
                    Console.WriteLine("Press any key to return to menu");
                    Console.ReadKey();
                    WriteMenu(items, items[index]);
                },
                ConsoleKey.Escape => () => isRunning = false,
                _ => () => { }
            };
            nextAction();
        }

        Console.ReadKey();
    }

    private static void WriteMenu(List<ConsoleMenuItem> items, ConsoleMenuItem selectedItem)
    {
        Console.Clear();

        foreach (var item in items)
        {
            Console.Write(item == selectedItem ? "> " : " ");
            Console.WriteLine(item.Name);
        }
    }
}
