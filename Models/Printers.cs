namespace PrintFlow_V2.Models;

public class Printer
{
    private readonly FileSystemWatcher _popWatcher;
    private readonly FileSystemWatcher _copWatcher;

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

    public string PadName => Name.PadRight(MaxLen);

    public Printer(string name, string copPath, string popPath, int maxLen)
    {
        Name = name;
        CopPath = copPath;
        PopPath = popPath;
        MaxLen = maxLen;

        _copWatcher = new FileSystemWatcher(copPath) { EnableRaisingEvents = true };
        _popWatcher = new FileSystemWatcher(popPath) { EnableRaisingEvents = true };

        _copWatcher.Renamed += OnFileRenamed;
        _popWatcher.Renamed += OnFileRenamed;
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
