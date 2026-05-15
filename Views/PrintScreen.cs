using System.Net;
using PrintFlow_V2.Models;
using PrintFlow_V2.UI;
using Spectre.Console;

namespace PrintFlow_V2.Views;

public class PrintScreen(PrintState state)
{
    private readonly PrintState _state = state;

    public void Show()
    {
        while (true)
        {
            AnsiConsole.Clear();
            ShowDashboard();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>().AddChoices(
                    "Select Files",
                    "View Queue",
                    "Send Files",
                    "Back"
                )
            );

            switch (choice)
            {
                case "Select Files":
                    HandleSelectFiles();
                    break;
                case "View Queue":
                    HandleViewQueue();
                    break;
                case "Send Files":
                    HandleSendFiles();
                    break;
                case "Back":
                    return;
            }
        }
    }

    private void ShowDashboard()
    {
        var queueTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(new Style(Color.Grey))
            .Title("[rgb(100,180,255)]Printer Queues[/]")
            .AddColumn(new TableColumn("Printer").Centered())
            .AddColumn(new TableColumn("Labels").Centered())
            .AddColumn(new TableColumn("Active").Centered())
            .AddColumn(new TableColumn("Queued").Centered())
            .AddColumn(new TableColumn("Staged").Centered())
            .AddColumn(new TableColumn("Status").Centered());

        foreach (var printer in _state.Printers)
        {
            var status = printer.Staged.Count > 0 ? "[green]● Printing[/]" : "[dim]● Idle[/]";

            queueTable.AddRow(
                printer.PadName,
                printer.TotalLabels.ToString("N0"),
                printer.Active.Count.ToString(),
                printer.Queued.Count.ToString(),
                printer.Staged.Count.ToString(),
                status
            );
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(queueTable));
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();

        AnsiConsole.Write(
            Align.Center(new Markup($"[dim]{_state.AvailableFiles.Count} file(s) available[/]"))
        );

        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine();
    }

    private void HandleSelectFiles()
    {
        if (_state.AvailableFiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No files available[/]");
            Console.ReadKey(true);
            return;
        }

        var prompt = new MultiSelectionPrompt<string>()
            .Title("[bold]Select files[/]")
            .InstructionsText("[dim]Space to toggle, Enter to confirm (empty to cancel)[/]")
            .Required(false);

        prompt.AddChoice("Select All");

        int maxLen = _state.AvailableFiles.Max(n => n.FileName.Length);
        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("[bold]Select files[/]")
                .InstructionsText("[dim]Space to toggle, Enter to confirm (empty to cancel)[/]")
                .Required(false)
                .AddChoices(
                    _state
                        .AvailableFiles.Select(f =>
                            $"{f.FileName.PadRight(maxLen)}  {f.Description} ({f.LabelCount:N0})"
                        )
                        .Prepend("Select All")
                )
        );
        if (selected.Count == 0)
            return;
        List<LabelFile> files = [];
        if (selected.Contains("Select All"))
        {
            files = [.. _state.AvailableFiles];
        }
        else
        {
            files =
            [
                .. _state.AvailableFiles.Where(f => selected.Any(s => s.StartsWith(f.FileName))),
            ];
        }

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]Action for {files.Count} file(s)[/]")
                .AddChoices("Assign to Printer", "Split File", "Clone", "Delete", "Cancel")
        );

        switch (action)
        {
            case "Assign to Printer":
                var printerChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[bold]Select printer[/]")
                        .AddChoices(
                            _state.Printers.Select(p => $"{p.PadName}  ({p.TotalLabels:N0} labels)")
                        )
                );

                var printer = _state.Printers.First(p => printerChoice.StartsWith(p.Name));
                _state.AssignToPrinter(files, printer);
                AnsiConsole.MarkupLine(
                    $"[green]Assigned {files.Count} file(s) to {printer.Name}[/]"
                );
                Console.ReadKey(true);
                break;

            case "Split File":
                if (files.Count > 1)
                {
                    AnsiConsole.MarkupLine("[red]Can only split one file at a time[/]");
                    Console.ReadKey(true);
                }
                else
                {
                    var chunks = AnsiConsole.Prompt(
                        new TextPrompt<int>("[bold]Split into how many chunks?[/]")
                            .DefaultValue(2)
                            .Validate(n =>
                                n >= 2 && n <= 100
                                    ? ValidationResult.Success()
                                    : ValidationResult.Error("Between 2 and 100")
                            )
                    );
                    _state.SplitFile(files[0], chunks);
                    AnsiConsole.MarkupLine($"[green]Split into {chunks} chunks[/]");
                    Console.ReadKey(true);
                }
                break;

            case "Clone":
                foreach (var file in files)
                    _state.CloneFile(file);
                AnsiConsole.MarkupLine($"[green]Cloned {files.Count} file(s)[/]");
                Console.ReadKey(true);
                break;
            case "Delete":
                if (AnsiConsole.Confirm($"[red]Delete {files.Count} file(s)?[/]", false))
                {
                    _state.DeleteFiles(files);
                    AnsiConsole.MarkupLine($"[green]Deleted[/]");
                    Console.ReadKey(true);
                }
                break;
        }
    }

    private void HandleSendFiles() => _state.SendStagedFiles();

    private void HandleViewQueue()
    {
        var printerChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Select printer to view[/]")
                .AddChoices(
                    _state
                        .Printers.Select(p => $"{p.PadName} ({p.TotalLabels:N0} labels)")
                        .Append("Cancel")
                )
        );

        if (printerChoice == "Cancel")
            return;

        var printer = _state.Printers.First(p => printerChoice.StartsWith(p.Name));

        if (printer.Staged.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{printer.Name} queue is empty[/]");
            Console.ReadKey(true);
            return;
        }

        int maxLen = printer.Staged.Max(n => n.FileName.Length);
        var toRemove = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"[bold]Select files to remove from {printer.Name}[/]")
                .InstructionsText("[dim]Space to toggle, Enter to confirm (empty to cancel)[/]")
                .Required(false)
                .AddChoices(
                    printer
                        .Staged.Select(f =>
                            $"{f.FileName.PadRight(maxLen)}  {f.Description} ({f.LabelCount:N0})"
                        )
                        .Prepend("Select All")
                )
        );

        if (toRemove.Count == 0)
            return;

        List<LabelFile> files = [];

        if (toRemove.Contains("Select All"))
        {
            files = [.. printer.Staged];
        }
        else
        {
            files = [.. printer.Staged.Where(f => toRemove.Any(s => s.StartsWith(f.FileName)))];
        }

        if (files.Count > 0)
        {
            _state.RemoveFromQueue(files, printer);

            var markups = files.Select((f, i) => new Markup($"{i + 1}. {f.FileName}"));

            Panels.MarkupList(markups, $"Removed from {printer.Name}", "yellow", Color.Yellow);
        }
    }
}
