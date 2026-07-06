namespace PrintFlow_V2.Models;

using PrintFlow_V2.Config;
using PrintFlow_V2.Services;

public class PrintState(PathSchema pathSchema)
{
    public PathSchema pathSchema = pathSchema;
    public List<LabelFile> AvailableFiles { get; } = [];
    public List<Printer> Printers { get; } = [];
    private FolderWatcher? _watcher;
    private readonly Lock _lock = new();

    public void Initialize()
    {
        AvailableFiles.AddRange(LabelService.GetLabels(pathSchema));

        _watcher = new FolderWatcher(pathSchema.LabelDataLoad.Path);

        _watcher.FileCreated += label =>
        {
            if (!AvailableFiles.Any(f => f.FilePath == label.FilePath))
                AvailableFiles.Add(label);
        };

        _watcher.FileDeleted += (label) => AvailableFiles.Remove(label);
    }

    public void RemoveFiles(List<LabelFile> files)
    {
        lock (_lock)
            AvailableFiles.RemoveAll(af => files.Any(f => f.Id == af.Id));
    }

    public void AddFiles(List<LabelFile> files)
    {
        lock (_lock)
            AvailableFiles.AddRange(files.Where(f => !AvailableFiles.Contains(f)));
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
