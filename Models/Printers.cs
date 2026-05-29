namespace PrintFlow_V2.Models;

using PrintFlow_V2.Services;

public class Printer
{
    private static readonly string _testBaseDir = @"C:\Temp\Printers";
    private readonly FileSystemWatcher _watcher;

    public string Name { get; }
    public string DirPath { get; }
    public int MaxLen { get; }

    public List<LabelFile> Queued { get; } = [];
    public List<LabelFile> Staged { get; } = [];
    public List<LabelFile> Active { get; } = [];

    public int TotalLabels =>
        Staged.Sum(f => f.LabelCount)
        + Queued.Sum(f => f.LabelCount)
        + Active.Sum(f => f.LabelCount);

    public string PadName => Name.PadRight(MaxLen);
    public string TestPath => Path.Combine(_testBaseDir, Path.GetFileName(DirPath));

    public event Action<LabelFile>? FileRenamed;

    public Printer(string name, string path, int maxLen)
    {
        Name = name;
        DirPath = path;
        MaxLen = maxLen;

        _watcher = new FileSystemWatcher(TestPath);
        _watcher.Renamed += OnFileRenamed;
    }

    public void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        string ext = Path.GetExtension(e.FullPath);
        LabelFile? label = Queued.FirstOrDefault(l => l.FilePath == e.OldFullPath);

        if (label != null)
        {
            if (ext == ".Processed")
            {
                Queued.RemoveAll(l => l.Id == label.Id);
                label.FilePath = e.FullPath;
                Active.Add(label);
            }

            if (ext == ".Completed")
                Active.RemoveAll(l => l.Id == label.Id);
        }
    }
}

// Staged: files the user is adding
// Queue: files in the printer dir that DON'T end in .Processed or .Complete
// Active: files in the printer dir that DO end in .Proccessed and not .Complete
//
//
// NEED: folder path for FBCOP and POP for each printer
