using PrintFlow_V2.Services;

namespace PrintFlow_V2.Models;

public class PrintState
{
    public List<LabelFile> AvailableFiles { get; } = [];
    public List<Printer> Printers { get; } = [];
    private FolderWatcher? _watcher;

    public void Initialize(string path)
    {
        AvailableFiles.AddRange(LabelService.GetLabels(path));

        _watcher = new FolderWatcher(path);

        _watcher.FileCreated += AvailableFiles.Add;
        _watcher.FileDeleted += (label) => AvailableFiles.Remove(label);
    }

    /// <summary>
    /// Moves selected files from available to a printer's queue.
    /// </summary>
    public void AssignToPrinter(List<LabelFile> files, Printer printer)
    {
        foreach (var file in files)
        {
            AvailableFiles.Remove(file);
            printer.Staged.Add(file);
        }
    }

    /// <summary>
    /// Moves files from a printer's queue back to available.
    /// </summary>
    public void RemoveFromQueue(List<LabelFile> files, Printer printer)
    {
        foreach (var file in files)
        {
            printer.Staged.Remove(file);
            AvailableFiles.Add(file);
        }
    }

    // foreach printer get each file in the queue and move it to the correct printer dest dir
    public void SendStagedFiles()
    {
        foreach (var printer in Printers)
        {
            foreach (LabelFile file in printer.Staged)
            {
                printer.Queued.Add(file);

                File.Move(
                    file.FilePath,
                    Path.Combine(printer.TestPath, Path.GetFileName(file.FilePath))
                );
            }

            printer.Staged.Clear();
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
