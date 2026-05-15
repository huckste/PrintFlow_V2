using PrintFlow_V2.Models;
using PrintFlow_V2.Services;
using Spectre.Console;

namespace PrintFlow_V2.Views;

public static class MainMenu
{
    public static List<MenuItem> Items()
    {
        return
        [
            new MenuItem(
                "Print",
                () =>
                {
                    var state = new PrintState();
                    List<Printer> printers = PrinterService.GetPrinters();
                    List<LabelFile> labels = LabelService.GetLabels();

                    state.AvailableFiles.AddRange(labels);
                    state.Printers.AddRange(printers);

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
                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine("[green]Reprint selected[/]");
                    AnsiConsole.MarkupLine("[dim]Press any key to return...[/]");
                    Console.ReadKey(true);
                },
                key: "e",
                icon: "\uf021"
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
