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

            var choice = Prompts.SingleSelect("", ["Select Files", "View Queue", "Send Files"]);

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
                case null:
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
            Messages.Warning("No files available");
            return;
        }

        int maxLen = _state.AvailableFiles.Max(n => n.FileName.Length);

        var selected = Prompts.MultiSelect(
            "Select files",
            _state.AvailableFiles.Select(f =>
                $"{f.FileName.PadRight(maxLen)}  {f.Description} ({f.LabelCount:N0})"
            ),
            "Space to toggle, Enter to confirm (empty to cancel)"
        );

        if (selected == null)
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

        var action = Prompts.SingleSelect(
            "Action for {files.Count} file(s)",
            ["Assign to Printer", "Split File", "Clone", "Delete"]
        );

        switch (action)
        {
            case "Assign to Printer":
                var printerChoice = Prompts.SingleSelect(
                    "Select printer",
                    [.. _state.Printers.Select(p => $"{p.PadName}  ({p.TotalLabels:N0} labels)")]
                );

                if (printerChoice == null)
                    return;

                var printer = _state.Printers.First(p => printerChoice.StartsWith(p.Name));
                _state.AssignToPrinter(files, printer);

                Messages.Success($"Assigned {files.Count} file(s) to {printer.Name}");
                break;

            case "Split File":
                if (files.Count > 1)
                {
                    Messages.Error("Can only split one file at a time");
                }
                else
                {
                    int chunks = Prompts.ValidateInt("Split into how many chunks?", 2, 2, 100);

                    _state.SplitFile(files[0], chunks);
                    Messages.Success($"Split into {chunks} chunks");
                }
                break;

            case "Clone":
                foreach (var file in files)
                    _state.CloneFile(file);

                Messages.Success($"Cloned {files.Count} file(s)");
                break;
            case "Delete":
                if (Prompts.Confirm($"Delete {files.Count} file(s)?", false))
                {
                    _state.DeleteFiles(files);
                    Messages.Success("Deleted");
                }
                break;
        }
    }

    private void HandleSendFiles() => _state.SendStagedFiles();

    private void HandleViewQueue()
    {
        var printerChoice = Prompts.SingleSelect(
            "Select printer to view",
            _state.Printers.Select(p => $"{p.PadName} ({p.TotalLabels:N0} labels)")
        );

        if (printerChoice == null)
            return;

        var printer = _state.Printers.First(p => printerChoice.StartsWith(p.Name));

        if (printer.Staged.Count == 0)
        {
            Messages.Warning($"{printer.Name} queue is empty");
            return;
        }

        int maxLen = printer.Staged.Max(n => n.FileName.Length);

        var toRemove = Prompts.MultiSelect(
            $"Select files to remove from {printer.Name}",
            printer.Staged.Select(f =>
                $"{f.FileName.PadRight(maxLen)}  {f.Description} ({f.LabelCount:N0})"
            )
        );

        if (toRemove == null)
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
