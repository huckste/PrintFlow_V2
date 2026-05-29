namespace PrintFlow_V2.Models;

public class LabelFile(string fileName, string filePath, string description, int labelCount)
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public string Description { get; set; } = description;
    public int LabelCount { get; set; } = labelCount;
    public string FileName { get; set; } = fileName;
    public string FilePath { get; set; } = filePath;

    public LabelFile Clone()
    {
        return new LabelFile($"{FileName}_copy", FilePath, Description, LabelCount);
    }

    public override bool Equals(object? obj) =>
        obj is LabelFile other && FilePath == other.FilePath;

    public override int GetHashCode() => FilePath.GetHashCode();
}
