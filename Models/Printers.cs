namespace PrintFlow_V2.Models;

public class Printer(string name, string path, int maxLen)
{
    private static readonly string _testBaseDir = @"C:\Temp\Printers";

    public string Name { get; } = name;
    public string DirPath { get; } = path;
    public int MaxLen { get; } = maxLen;

    public List<LabelFile> Queued { get; } = [];
    public List<LabelFile> Staged { get; } = [];
    public List<LabelFile> Active { get; } = [];

    public int TotalLabels =>
        Staged.Sum(f => f.LabelCount)
        + Queued.Sum(f => f.LabelCount)
        + Active.Sum(f => f.LabelCount);

    public string PadName => Name.PadRight(MaxLen);
    public string TestPath => Path.Combine(_testBaseDir, Path.GetFileName(DirPath));
}
// Staged: files the user is adding
// Queue: files in the printer dir that DON'T end in .Processed or .Complete
// Active: files in the printer dir that DO end in .Proccessed and not .Complete
