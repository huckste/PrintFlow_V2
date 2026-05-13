namespace PrintFlow_V2.Models;

public class LabelFile
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public string Description { get; set; }
    public int LabelCount { get; set; }
    public string FileName { get; set; }
    public string WaveNumber => FileName.Split('.', '_')[0];

    public LabelFile(string fileName, string description, int labelCount)
    {
        Description = description;
        LabelCount = labelCount;
        FileName = fileName;
    }

    public LabelFile Clone()
    {
        return new LabelFile($"{FileName}_copy", Description, LabelCount);
    }
}
