using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;
using PrintFlow_V2.UI;
using Spectre.Console;

namespace PrintFlow_V2.Views;

public static class MainMenu
{
    public static List<MenuItem> Items(PrintState state)
    {
        return
        [
            new MenuItem(
                "Print",
                () =>
                {
                    var screen = new PrintScreen(state);
                    screen.Show();
                },
                key: "p",
                icon: "\uf02f"
            ),
            new MenuItem(
                "Release",
                () =>
                {
                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine("[green]Release selected[/]");
                    AnsiConsole.MarkupLine("[dim]Press any key to return...[/]");
                    Console.ReadKey(true);
                },
                key: "r",
                icon: "\uf49e"
            ),
            new MenuItem(
                "Reprint",
                () =>
                {
                    var screen = new ReprintScreen(state);
                    screen.Show();
                },
                key: "e",
                icon: "\uf021"
            ),
            new MenuItem(
                "Config",
                () =>
                {
                    var pathSchema = ConfigManager.Load().LogOnError();

                    if (!pathSchema.IsError)
                    {
                        var configMenu = new ConfigMenu(pathSchema.Value);
                        configMenu.Run();
                    }
                },
                key: "c",
                icon: "\uf013"
            ),
            new MenuItem(
                "Quit",
                () =>
                {
                    Environment.Exit(0);
                },
                key: "q",
                icon: "\uf426"
            ),
        ];
    }
}
