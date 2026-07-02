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
            var status = printer.Active.Count > 0 ? "[green]● Processing[/]" : "[dim]● Idle[/]";

            queueTable.AddRow(
                printer.PadName,
                printer.TotalLabels.ToString("N0"),
                printer.Active.Count.ToString(),
                printer.Queued.Count.ToString(),
                printer.Staged.Count.ToString(),
                status
            );
        }

        Messages.Empty(1);
        AnsiConsole.Write(Align.Center(queueTable));
        Messages.Empty(2);

        AnsiConsole.Write(
            Align.Center(new Markup($"[dim]{_state.AvailableFiles.Count} file(s) available[/]"))
        );

        Messages.Empty(4);
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
            )
        );

        if (selected == null)
            return;

        List<LabelFile> files = [];

        files = [.. _state.AvailableFiles.Where(f => selected.Any(s => s.StartsWith(f.FileName)))];

        var action = Prompts.SingleSelect(
            $"Action for {files.Count} file(s)",
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

                var printer = _state.Printers.First(p => printerChoice.StartsWith(p.PadName));
                _state.AssignToPrinter(files, printer);

                Messages.Success($"Assigned {files.Count} file(s) to {printer.Name}");
                break;

            case "Split File":
                if (files.Count > 1)
                {
                    Messages.Warning("Can only split one file at a time");
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

    private void HandleSendFiles()
    {
        var printers = _state.Printers.Where(p => p.Staged.Count > 0).ToList();

        if (printers.Count > 0)
        {
            _state.SendStagedFiles();
        }
        else
        {
            Messages.Warning("No files staged");
        }
    }

    private void HandleViewQueue()
    {
        // Need to show the files in the QUEUED list along with the files in Staged
        // If you want to remove a file from QUEUED you need to remove from the QUEUED list and from the Printers folder back to available
        //
        var printerChoice = Prompts.SingleSelect(
            "Select printer to view",
            _state.Printers.Select(p => $"{p.PadName} ({p.TotalLabels:N0} labels)")
        );

        if (printerChoice == null)
            return;

        var printer = _state.Printers.First(p => printerChoice.StartsWith(p.PadName));

        if (printer.Staged.Count == 0 && printer.Queued.Count == 0)
        {
            Messages.Warning($"{printer.Name} queue is empty");
            return;
        }

        int maxLen = 0;
        int stagedLen = 0;
        int queueLen = 0;

        if (printer.Staged.Count > 0)
            stagedLen = printer.Staged.Max(n => n.FileName.Length);

        if (printer.Queued.Count > 0)
            queueLen = printer.Queued.Max(n => n.FileName.Length);

        maxLen = stagedLen > queueLen ? stagedLen : queueLen;

        List<string> allFiles = [];

        allFiles.AddRange([
            .. printer.Staged.Select(f =>
                $"{f.FileName.PadRight(maxLen)}  {f.Description} ({f.LabelCount:N0})"
            ),
        ]);

        allFiles.AddRange([
            .. printer.Queued.Select(f =>
                $"[yellow]{f.FileName.PadRight(maxLen)}  {f.Description} ({f.LabelCount:N0})[/]"
            ),
        ]);

        var toRemove = Prompts.MultiSelect($"Select files to remove from {printer.Name}", allFiles);

        if (toRemove == null)
            return;

        List<Markup> markups = [];
        List<LabelFile> stagedFiles = [];
        List<LabelFile> queuedFiles = [];

        // TODO: need to find which display strings did not match any files in staged or queued as them must be in the active array now. Diplay to the user that they can't remvoe that file from the queue

        stagedFiles = [.. printer.Staged.Where(f => toRemove.Any(s => s.StartsWith(f.FileName)))];

        queuedFiles =
        [
            .. printer.Queued.Where(f =>
                toRemove.Any(s => s.Replace("[yellow]", "").StartsWith(f.FileName))
            ),
        ];

        if (stagedFiles.Count > 0)
        {
            _state.RemoveFromStaged(stagedFiles, printer);

            markups.AddRange([
                .. stagedFiles.Select((f, i) => new Markup($"{i + 1}. {f.FileName}")),
            ]);
        }

        if (queuedFiles.Count > 0)
        {
            _state
                .RemoveFromQueue(queuedFiles, printer)
                .Switch(
                    value =>
                        markups.AddRange([
                            .. queuedFiles.Select((f, i) => new Markup($"{i + 1}. {f.FileName}")),
                        ]),
                    Messages.Error
                );
        }

        if (markups.Count > 0)
            Panels.MarkupList(markups, $"Removed from {printer.Name}", "yellow", Color.Yellow);
    }
}
