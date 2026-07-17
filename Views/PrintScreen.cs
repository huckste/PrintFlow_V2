using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;
using PrintFlow_V2.Services;
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
            var status =
                printer.GetQueueCount(Printer.PrinterQueue.Active) > 0
                    ? "[green]● Processing[/]"
                    : "[dim]● Idle[/]";

            queueTable.AddRow(
                printer.PadName,
                printer.TotalLabels.ToString("N0"),
                printer.GetQueueCount(Printer.PrinterQueue.Active).ToString(),
                printer.GetQueueCount(Printer.PrinterQueue.Queued).ToString(),
                printer.GetQueueCount(Printer.PrinterQueue.Staged).ToString(),
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
        // TODO: call EnsureReady() here — show AnsiConsole.Status spinner while _state.InitTask completes before accessing AvailableFiles
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

        List<LabelFile> files =
        [
            .. _state.AvailableFiles.Where(f => selected.Any(s => s.StartsWith(f.FileName))),
        ];

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

                PrinterService.AssignToPrinter(_state, files, printer);

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
        var printers = _state
            .Printers.Where(p => p.GetQueueCount(Printer.PrinterQueue.Staged) > 0)
            .ToList();

        if (printers.Count > 0)
        {
            foreach (var printer in printers)
            {
                var result = PrinterService.SendStagedFiles(printer).LogOnError();
                Dictionary<string, List<Markup>> markups = [];

                if (!result.IsError)
                {
                    int i = 1;
                    markups.TryAdd("success", []);

                    foreach (LabelFile file in result.Value)
                        markups["success"].Add(new Markup($"{i++}. {file.FileName}"));

                    if (markups["success"].Count > 0)
                        Panels.MarkupList(
                            markups["success"],
                            $"Sent to {printer.Name}",
                            "green",
                            Color.Green
                        );
                }
                else
                {
                    int i = 1;
                    markups.TryAdd("failed", []);

                    foreach (var error in result.Errors)
                        markups["failed"].Add(new Markup($"{i++}. {error.Description}"));

                    if (markups["failed"].Count > 0)
                        Panels.MarkupList(markups["failed"], "Error", "red", Color.Red);
                }
            }
        }
        else
        {
            Messages.Warning("No files staged");
        }
    }

    private void HandleViewQueue()
    {
        // TODO: call EnsureReady() here — show AnsiConsole.Status spinner while _state.InitTask completes before accessing queues
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

        if (printer.GetAllQueueCount() is null)
        {
            Messages.Warning($"{printer.Name} queue is empty");
            return;
        }

        int maxLen = printer.GetMaxFileNameAllQueues();

        List<string> allFiles = [];

        foreach (var queue in Enum.GetValues<Printer.PrinterQueue>())
        {
            string textColor = queue switch
            {
                Printer.PrinterQueue.Staged => "[white]",
                Printer.PrinterQueue.Queued => "[yellow]",
                Printer.PrinterQueue.Active => "[green]",
                _ => "[grey]",
            };

            allFiles.AddRange([
                .. printer
                    .QueueSnapshot(queue)
                    .Select(f =>
                        $"{textColor}{f.FileName.PadRight(maxLen)}  {f.Description} ({f.LabelCount:N0})[/]"
                    ),
            ]);
        }

        var toRemove = Prompts.MultiSelect($"Select files to remove from {printer.Name}", allFiles);

        if (toRemove == null)
            return;

        Dictionary<string, List<Markup>> markups = [];

        // TODO: need to find which display strings did not match any files in staged or queued as them must be in the active array now. Diplay to the user that they can't remvoe that file from the queue

        foreach (var queue in Enum.GetValues<Printer.PrinterQueue>())
        {
            string textColor = queue switch
            {
                Printer.PrinterQueue.Staged => "[white]",
                Printer.PrinterQueue.Queued => "[yellow]",
                Printer.PrinterQueue.Active => "[green]",
                _ => "[grey]",
            };

            List<LabelFile> files =
            [
                .. printer
                    .QueueSnapshot(queue)
                    .Where(f => toRemove.Any(s => s.Replace(textColor, "").StartsWith(f.FileName))),
            ];

            switch (queue)
            {
                case Printer.PrinterQueue.Staged:
                    PrinterService.RemoveFromStaged(_state, files, printer);
                    markups["success"] =
                    [
                        .. files.Select((f, i) => new Markup($"{i + 1}. {f.FileName}")),
                    ];
                    break;
                case Printer.PrinterQueue.Queued:
                    PrinterService
                        .RemoveFromQueue(_state.pathSchema, _state, files, printer)
                        .Switch(
                            value =>
                                markups["success"]
                                    .AddRange([
                                        .. files.Select(
                                            (f, i) => new Markup($"{i + 1}. {f.FileName}")
                                        ),
                                    ]),
                            Messages.Error
                        );
                    break;
                case Printer.PrinterQueue.Active:
                    markups["failed"] =
                    [
                        .. files.Select((f, i) => new Markup($"{1 + 1}. {f.FileName}")),
                    ];
                    break;
            }
        }

        foreach (var (key, value) in markups)
        {
            if (key == "success" && value.Count > 0)
                Panels.MarkupList(value, $"Removed from {printer.Name}", "yellow", Color.Yellow);

            if (key == "failed" && value.Count > 0)
                Panels.MarkupList(value, $"Can't remove from {printer.Name}", "red", Color.Red);
        }
    }
}
