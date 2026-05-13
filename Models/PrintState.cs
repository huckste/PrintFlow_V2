namespace PrintFlow_V2.Models;

public class PrintState
{
    public List<LabelFile> AvailableFiles { get; } = [];
    public List<Printer> Printers { get; } = [];

    /// <summary>
    /// Moves selected files from available to a printer's queue.
    /// </summary>
    public void AssignToPrinter(List<LabelFile> files, Printer printer)
    {
        foreach (var file in files)
        {
            AvailableFiles.Remove(file);
            printer.Queue.Add(file);
        }
    }

    /// <summary>
    /// Moves files from a printer's queue back to available.
    /// </summary>
    public void RemoveFromQueue(List<LabelFile> files, Printer printer)
    {
        foreach (var file in files)
        {
            printer.Queue.Remove(file);
            AvailableFiles.Add(file);
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
                new LabelFile($"{file.FileName}_pt{i + 1}", file.Description, count)
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
