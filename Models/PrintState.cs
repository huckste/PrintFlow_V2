namespace PrintFlow_V2.Models;

using PrintFlow_V2.Services;
using PrintFlow_V2.UI;
using Spectre.Console;

public class PrintState
{
    public List<LabelFile> AvailableFiles { get; } = [];
    public List<Printer> Printers { get; } = [];
    private FolderWatcher? _watcher;
    private readonly string _labelDataLoad = @"C:/Temp/Label_Data_Load";

    public void Initialize(string? path)
    {
        AvailableFiles.AddRange(LabelService.GetLabels(path));

        _watcher = new FolderWatcher(_labelDataLoad);

        _watcher.FileCreated += label =>
        {
            if (!AvailableFiles.Any(f => f.FilePath == label.FilePath))
                AvailableFiles.Add(label);
        };

        _watcher.FileDeleted += (label) => AvailableFiles.Remove(label);
    }

    public void AssignToPrinter(List<LabelFile> files, Printer printer)
    {
        foreach (var file in files)
        {
            AvailableFiles.Remove(file);
            printer.Staged.Add(file);
        }
    }

    public void RemoveFromQueue(List<LabelFile> files, Printer printer)
    {
        foreach (var file in files)
        {
            printer.Queued.Remove(file);

            string dest = Path.Combine(_labelDataLoad, Path.GetFileName(file.FilePath));
            File.Move(file.FilePath, dest);

            file.FilePath = dest;
            AvailableFiles.Add(file);
        }
    }

    public void RemoveFromStaged(List<LabelFile> files, Printer printer)
    {
        foreach (var file in files)
        {
            printer.Staged.Remove(file);
            AvailableFiles.Add(file);
        }
    }

    // adjust the new path, save the new path for the current file, and move the file to the proper printer dir
    public void SendStagedFiles()
    {
        foreach (var printer in Printers)
        {
            List<Markup> markups = [];
            int i = 1;

            foreach (LabelFile file in printer.Staged)
            {
                string dest = Path.Combine(printer.TestPath, Path.GetFileName(file.FilePath));
                File.Move(file.FilePath, dest);

                file.FilePath = dest;

                markups.Add(new Markup($"{i++}. {file.FileName}"));

                printer.Queued.Add(file);
            }

            printer.Staged.Clear();

            if (markups.Count > 0)
                Panels.MarkupList(markups, $"Sent to {printer.Name}", "green", Color.Green);
        }
    }

    /// <summary>
    /// Splits a file into N equal chunks, replaces original in available list.
    /// </summary>
    public void SplitFile(LabelFile file, int chunks)
    {
        var index = AvailableFiles.IndexOf(file);

        if (index < 0)
            return;

        AvailableFiles.RemoveAt(index);

        var perChunk = file.LabelCount / chunks;
        var remainder = file.LabelCount % chunks;

        for (var i = 0; i < chunks; i++)
        {
            var count = perChunk + (i < remainder ? 1 : 0);

            AvailableFiles.Insert(
                index + i,
                new LabelFile($"{file.FileName}_pt{i + 1}", file.FilePath, file.Description, count)
            );
        }
    }

    /// <summary>
    /// Clones a file and adds the copy to available list.
    /// </summary>
    public void CloneFile(LabelFile file)
    {
        var index = AvailableFiles.IndexOf(file);
        if (index < 0)
            return;

        AvailableFiles.Insert(index + 1, file.Clone());
    }

    /// <summary>
    /// Permanently deletes files from available list.
    /// </summary>
    public void DeleteFiles(List<LabelFile> files)
    {
        foreach (var file in files)
            AvailableFiles.Remove(file);
    }
}
