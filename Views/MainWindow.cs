using System.Collections.ObjectModel;
using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;
using PrintFlow_V2.Services;
using PrintFlow_V2.UI;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TGuiMenuItem = Terminal.Gui.Views.MenuItem;

namespace PrintFlow_V2.Views;

public class MainWindow : Window
{
    private enum ContentMode
    {
        Files,
        Queue,
    }

    private PrintState? _state;
    private ContentMode _mode = ContentMode.Files;
    private bool _assignMode;
    private Printer? _viewingPrinter;
    private List<LabelFile> _pendingFiles = [];
    private List<(LabelFile File, Printer.PrinterQueue Queue)> _queueItems = [];

    private readonly FrameView _statusFrame;
    private readonly ListView _printerList;
    private readonly FrameView _contentFrame;
    private readonly ListView _contentList;

    private readonly View _filesModeBar;
    private readonly View _assignModeBar;
    private readonly View _queueModeBar;

    public MainWindow()
    {
        Title = "PrintFlow";

        var menuBar = new MenuBar
        {
            X = Pos.Center(),
            Width = Dim.Percent(80),
            Menus =
            [
                new MenuBarItem(
                    "_Config",
                    new TGuiMenuItem[] { new("_Open Config", "", OpenConfig) }
                ),
                new MenuBarItem("_Release", new TGuiMenuItem[] { new("_Release", "", () => { }) }),
                new MenuBarItem(
                    "_Reprint",
                    new TGuiMenuItem[]
                    {
                        new("_By Header", "", () => { }),
                        new("_By Page Number", "", () => { }),
                    }
                ),
                new MenuBarItem(
                    "_Quit",
                    new TGuiMenuItem[]
                    {
                        new("_Quit", "", () => TuiApp.App.RequestStop()),
                    }
                ),
            ],
        };

        _statusFrame = new FrameView
        {
            Title = "Printer Status",
            X = Pos.Center(),
            Y = 2,
            Width = 60,
            Height = 4,
            CanFocus = true,
        };

        _printerList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };
        _printerList.Activated += OnPrinterActivated;
        _statusFrame.Add(_printerList);

        _contentFrame = new FrameView
        {
            Title = "Available Files",
            X = Pos.Center(),
            Y = Pos.Bottom(_statusFrame) + 1,
            Width = 60,
            Height = Dim.Fill(5),
            CanFocus = true,
        };

        _contentList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShowMarks = true,
            MarkMultiple = true,
            CanFocus = true,
        };
        _contentList.HasFocusChanged += (_, e) =>
        {
            if (e.NewFocused == _contentList)
                ShowFileModeButtons();
        };
        _contentFrame.Add(_contentList);

        // --- Files mode buttons ---
        var assignBtn = new Button { Text = "Assign to Printer", X = 0, Y = 0 };
        var splitBtn = new Button { Text = "Split", X = Pos.Right(assignBtn) + 1, Y = 0 };
        var cloneBtn = new Button { Text = "Clone", X = Pos.Right(splitBtn) + 1, Y = 0 };
        var deleteBtn = new Button { Text = "Delete", X = Pos.Right(cloneBtn) + 1, Y = 0 };
        var viewQueueBtn = new Button
        {
            Text = "View Queue",
            X = Pos.Right(deleteBtn) + 3,
            Y = 0,
        };
        var sendFilesBtn = new Button
        {
            Text = "Send Files",
            X = Pos.Right(viewQueueBtn) + 1,
            Y = 0,
        };

        assignBtn.Accepting += (_, _) => HandleAssign();
        splitBtn.Accepting += (_, _) => HandleSplit();
        cloneBtn.Accepting += (_, _) => HandleClone();
        deleteBtn.Accepting += (_, _) => HandleDelete();
        viewQueueBtn.Accepting += (_, _) => HandleViewQueue();
        sendFilesBtn.Accepting += (_, _) => HandleSendFiles();

        _filesModeBar = new View
        {
            X = Pos.Center(),
            Y = Pos.Bottom(_contentFrame) + 2,
            Width = Dim.Auto(),
            Height = 1,
        };
        _filesModeBar.Add(assignBtn, splitBtn, cloneBtn, deleteBtn, viewQueueBtn, sendFilesBtn);

        // --- Assign mode buttons ---
        var cancelAssignBtn = new Button { Text = "Cancel", X = 0, Y = 0 };
        cancelAssignBtn.Accepting += (_, _) => CancelAssign();

        _assignModeBar = new View
        {
            X = Pos.Center(),
            Y = Pos.Bottom(_contentFrame) + 2,
            Width = Dim.Auto(),
            Height = 1,
            Visible = false,
        };
        _assignModeBar.Add(cancelAssignBtn);

        // --- Queue mode buttons ---
        var backBtn = new Button { Text = "Back", X = 0, Y = 0 };
        var removeBtn = new Button
        {
            Text = "Remove from Queue",
            X = Pos.Right(backBtn) + 2,
            Y = 0,
        };

        backBtn.Accepting += (_, _) => SwitchToFiles();
        removeBtn.Accepting += (_, _) => HandleRemoveFromQueue();

        _queueModeBar = new View
        {
            X = Pos.Center(),
            Y = Pos.Bottom(_contentFrame) + 2,
            Width = Dim.Auto(),
            Height = 1,
            Visible = false,
        };
        _queueModeBar.Add(backBtn, removeBtn);

        Add(menuBar, _statusFrame, _contentFrame, _filesModeBar, _assignModeBar, _queueModeBar);

        Initialized += OnInitialized;
    }

    private void OnInitialized(object? sender, EventArgs e)
    {
        var schema = PrintFlowApp.EnsureConfig();

        if (schema is null)
        {
            TuiApp.App.RequestStop();
            return;
        }

        _state = new PrintState(schema);
        _state.StateChanged += () => TuiApp.App.Invoke(Refresh);

        var printers = PrinterService.GetPrinters(schema).LogOnError();

        if (!printers.IsError)
            _state.Initialize(printers.Value);

        Refresh();
    }

    // --- Mode switching ---

    private void ShowFileModeButtons()
    {
        if (_mode != ContentMode.Files || _assignMode)
            return;

        _filesModeBar.Visible = true;
        _assignModeBar.Visible = false;
        _queueModeBar.Visible = false;
    }

    private void SwitchToFiles()
    {
        _mode = ContentMode.Files;
        _viewingPrinter = null;
        _assignMode = false;
        _contentList.ShowMarks = true;
        _contentList.MarkMultiple = true;
        _contentList.SelectedItem = null;
        _statusFrame.Title = "Printer Status";
        _filesModeBar.Visible = true;
        _assignModeBar.Visible = false;
        _queueModeBar.Visible = false;
        Refresh();
    }

    private void SwitchToQueue(Printer printer)
    {
        _mode = ContentMode.Queue;
        _viewingPrinter = printer;
        _assignMode = false;
        _contentList.ShowMarks = true;
        _contentList.MarkMultiple = true;
        _contentList.SelectedItem = null;
        _statusFrame.Title = "Printer Status";
        _filesModeBar.Visible = false;
        _assignModeBar.Visible = false;
        _queueModeBar.Visible = true;
        Refresh();
    }

    private void EnterAssignMode(List<LabelFile> files)
    {
        _assignMode = true;
        _pendingFiles = files;
        _statusFrame.Title = $"Click Printer to Assign — {files.Count} file(s)";
        _filesModeBar.Visible = false;
        _assignModeBar.Visible = true;
        _queueModeBar.Visible = false;
        _printerList.SetFocus();
    }

    private void CancelAssign()
    {
        _assignMode = false;
        _pendingFiles = [];
        _statusFrame.Title = "Printer Status";
        _filesModeBar.Visible = true;
        _assignModeBar.Visible = false;
        _queueModeBar.Visible = false;
    }

    // --- Refresh ---

    private void Refresh()
    {
        if (_state is null)
            return;

        var printerRows = _state.Printers.Select(FormatPrinterRow).ToList();
        _printerList.SetSource(new ObservableCollection<string>(printerRows));
        _statusFrame.Height = Math.Max(_state.Printers.Count, 1) + 2;

        List<string> contentRows = _mode switch
        {
            ContentMode.Queue when _viewingPrinter is not null => BuildQueueRows(_viewingPrinter),
            _ => BuildFileRows(),
        };

        _contentFrame.Title = _mode switch
        {
            ContentMode.Queue when _viewingPrinter is not null =>
                $"Queue — {_viewingPrinter.Name}",
            _ => $"Available Files ({_state.AvailableFiles.Count})",
        };

        _contentList.SetSource(new ObservableCollection<string>(contentRows));

        int printerWidth = printerRows.Count > 0 ? printerRows.Max(r => r.Length) : 40;
        int contentWidth = contentRows.Count > 0 ? contentRows.Max(r => r.Length) : 40;
        int panelWidth = Math.Max(printerWidth, contentWidth) + 4;

        _statusFrame.Width = panelWidth;
        _contentFrame.Width = panelWidth;
    }

    // --- Row builders ---

    private static string FormatPrinterRow(Printer printer)
    {
        string status =
            printer.GetQueueCount(Printer.PrinterQueue.Active) > 0 ? "● Processing" : "  Idle      ";

        return string.Format(
            " {0}  {1,8} labels   Act:{2}  Que:{3}  Stg:{4}   {5}",
            printer.PadName,
            printer.TotalLabels.ToString("N0"),
            printer.GetQueueCount(Printer.PrinterQueue.Active),
            printer.GetQueueCount(Printer.PrinterQueue.Queued),
            printer.GetQueueCount(Printer.PrinterQueue.Staged),
            status
        );
    }

    private List<string> BuildFileRows()
    {
        if (_state is null || _state.AvailableFiles.Count == 0)
            return [];

        int maxLen = _state.AvailableFiles.Max(f => f.FileName.Length);

        return _state
            .AvailableFiles.Select(f =>
                $" {f.FileName.PadRight(maxLen)}  {f.Description}  ({f.LabelCount:N0})"
            )
            .ToList();
    }

    private List<string> BuildQueueRows(Printer printer)
    {
        List<string> rows = [];
        _queueItems = [];

        int maxLen = printer.GetMaxFileNameAllQueues();
        if (maxLen == 0)
            maxLen = 20;

        foreach (var queue in new[]
        {
            Printer.PrinterQueue.Staged,
            Printer.PrinterQueue.Queued,
            Printer.PrinterQueue.Active,
        })
        {
            string prefix = queue switch
            {
                Printer.PrinterQueue.Active => "[Active] ",
                Printer.PrinterQueue.Queued => "[Queued] ",
                Printer.PrinterQueue.Staged => "[Staged] ",
                _ => "",
            };

            foreach (var file in printer.QueueSnapshot(queue))
            {
                rows.Add(
                    $" {prefix}{file.FileName.PadRight(maxLen)}  {file.Description} ({file.LabelCount:N0})"
                );
                _queueItems.Add((file, queue));
            }
        }

        if (rows.Count == 0)
            rows.Add(" (empty queue)");

        return rows;
    }

    // --- Printer list events ---

    private void OnPrinterActivated(object? sender, EventArgs e)
    {
        if (_state is null)
            return;

        var index = _printerList.SelectedItem;

        if (!index.HasValue)
            return;

        var printer = _state.Printers.ElementAtOrDefault(index.Value);

        if (printer is null)
            return;

        if (_assignMode)
        {
            PrinterService.AssignToPrinter(_state, _pendingFiles, printer);
            Messages.Success($"Assigned {_pendingFiles.Count} file(s) to {printer.Name}");
            _pendingFiles = [];
            SwitchToFiles();
        }
        else
        {
            SwitchToQueue(printer);
        }
    }

    // --- File operations ---

    private List<LabelFile> GetMarkedFiles()
    {
        if (_state is null)
            return [];

        return _contentList
            .GetAllMarkedItems()
            .Select(i => _state.AvailableFiles.ElementAtOrDefault(i))
            .Where(f => f is not null)
            .Cast<LabelFile>()
            .ToList();
    }

    private void HandleAssign()
    {
        var files = GetMarkedFiles();

        if (files.Count == 0)
        {
            Messages.Warning("No files selected — Space to mark files");
            return;
        }

        EnterAssignMode(files);
    }

    private void HandleSplit()
    {
        if (_state is null)
            return;

        var files = GetMarkedFiles();

        if (files.Count == 0)
        {
            Messages.Warning("No files selected — Space to mark files");
            return;
        }

        if (files.Count > 1)
        {
            Messages.Warning("Select one file to split");
            return;
        }

        var chunks = Dialogs.IntInput("Split into how many chunks?", 2, 2, 100);

        if (chunks is null)
            return;

        _state.SplitFile(files[0], chunks.Value);
        Refresh();
    }

    private void HandleClone()
    {
        if (_state is null)
            return;

        var files = GetMarkedFiles();

        if (files.Count == 0)
        {
            Messages.Warning("No files selected — Space to mark files");
            return;
        }

        foreach (var file in files)
            _state.CloneFile(file);

        Refresh();
    }

    private void HandleDelete()
    {
        if (_state is null)
            return;

        var files = GetMarkedFiles();

        if (files.Count == 0)
        {
            Messages.Warning("No files selected — Space to mark files");
            return;
        }

        if (!Dialogs.Confirm($"Delete {files.Count} file(s)?"))
            return;

        _state.DeleteFiles(files);
        Refresh();
    }

    private void HandleViewQueue()
    {
        if (_state is null)
            return;

        var index = _printerList.SelectedItem;
        Printer? printer = null;

        if (index.HasValue)
            printer = _state.Printers.ElementAtOrDefault(index.Value);

        if (printer is null && _state.Printers.Count == 1)
            printer = _state.Printers[0];

        if (printer is null)
        {
            Messages.Warning("Select a printer row first (click or arrow keys in the Printer Status panel)");
            return;
        }

        SwitchToQueue(printer);
    }

    private void HandleSendFiles()
    {
        if (_state is null)
            return;

        var staged = _state
            .Printers.Where(p => p.GetQueueCount(Printer.PrinterQueue.Staged) > 0)
            .ToList();

        if (staged.Count == 0)
        {
            Messages.Warning("No files staged for any printer");
            return;
        }

        int totalSent = 0;

        foreach (var printer in staged)
        {
            var result = PrinterService.SendStagedFiles(printer).LogOnError();

            if (!result.IsError)
                totalSent += result.Value.Count;
            else
                Messages.Error(result.Errors);
        }

        if (totalSent > 0)
            Messages.Success($"Sent {totalSent} file(s) to printer(s)");

        Refresh();
    }

    private void HandleRemoveFromQueue()
    {
        if (_state is null || _viewingPrinter is null)
            return;

        var markedIndices = _contentList.GetAllMarkedItems().ToList();

        if (markedIndices.Count == 0)
        {
            Messages.Warning("No files selected — Space to mark files");
            return;
        }

        var selected = markedIndices
            .Where(i => i < _queueItems.Count)
            .Select(i => _queueItems[i])
            .ToList();

        var toRemoveStaged = selected
            .Where(t => t.Queue == Printer.PrinterQueue.Staged)
            .Select(t => t.File)
            .ToList();

        var toRemoveQueued = selected
            .Where(t => t.Queue == Printer.PrinterQueue.Queued)
            .Select(t => t.File)
            .ToList();

        int activeCount = selected.Count(t => t.Queue == Printer.PrinterQueue.Active);

        if (toRemoveStaged.Count > 0)
            PrinterService.RemoveFromStaged(_state, toRemoveStaged, _viewingPrinter);

        if (toRemoveQueued.Count > 0)
        {
            var result = PrinterService.RemoveFromQueue(
                _state.pathSchema,
                _state,
                toRemoveQueued,
                _viewingPrinter
            );

            if (result.IsError)
                Messages.Error(result.Errors);
        }

        if (activeCount > 0)
            Messages.Warning($"Cannot remove {activeCount} active file(s) — already printing");

        int removed = toRemoveStaged.Count + toRemoveQueued.Count;

        if (removed > 0)
            Messages.Success($"Removed {removed} file(s)");

        Refresh();
    }

    private void OpenConfig()
    {
        var schema = _state?.pathSchema ?? new PathSchema();
        new ConfigMenu(schema).Run();
    }
}
