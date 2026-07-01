namespace PrintFlow_V2.Models;

public class Printer
{
    private readonly FileSystemWatcher _watcher;

    public string Name { get; }
    public string PopPath { get; }
    public string CopPath { get; }
    public int MaxLen { get; }

    public List<LabelFile> Queued { get; } = [];
    public List<LabelFile> Staged { get; } = [];
    public List<LabelFile> Active { get; } = [];

    public int TotalLabels =>
        Staged.Sum(f => f.LabelCount)
        + Queued.Sum(f => f.LabelCount)
        + Active.Sum(f => f.LabelCount);

    // Max sure all columns are lined up
    public string PadName => Name.PadRight(MaxLen);

    public event Action<LabelFile>? FileRenamed;

    public Printer(string name, string copPath, string popPath, int maxLen)
    {
        Name = name;
        CopPath = copPath;
        PopPath = popPath;
        MaxLen = maxLen;

        _watcher = new FileSystemWatcher(copPath) { EnableRaisingEvents = true };
        _watcher = new FileSystemWatcher(popPath) { EnableRaisingEvents = true };
        _watcher.Renamed += OnFileRenamed;
    }

    public void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        string ext = Path.GetExtension(e.FullPath);

        if (ext == ".Processed")
        {
            LabelFile? label = Queued.FirstOrDefault(l => l.FilePath == e.OldFullPath);

            if (label != null)
            {
                Queued.RemoveAll(l => l.Id == label.Id);
                label.FilePath = e.FullPath;
                Active.Add(label);
            }
        }

        if (ext == ".Completed")
        {
            LabelFile? label = Active.FirstOrDefault(l => l.FilePath == e.OldFullPath);

            if (label != null)
                Active.RemoveAll(l => l.Id == label.Id);
        }
    }
}
