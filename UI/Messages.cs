namespace PrintFlow_V2.UI;

using Spectre.Console;

public class Messages
{
    public static void Success(string message)
    {
        AnsiConsole.MarkupLine($"[green]{message}[/]");
        Console.ReadKey(true);
    }

    public static void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]{message}[/]");
        Console.ReadKey(true);
    }

    public static void Warning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{message}[/]");
        Console.ReadKey(true);
    }

    public static void Empty(int count)
    {
        for (int i = 0; i < count; i++)
        {
            AnsiConsole.WriteLine();
        }
    }
}
