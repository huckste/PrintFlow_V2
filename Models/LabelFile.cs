namespace PrintFlow_V2.Models;

public class LabelFile(string fileName, string description, int labelCount)
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
    public string Description { get; set; } = description;
    public int LabelCount { get; set; } = labelCount;
    public string FileName { get; set; } = fileName;
    public string WaveNumber => FileName.Split('.', '_')[0];

    public LabelFile Clone()
    {
        return new LabelFile($"{FileName}_copy", Description, LabelCount);
    }
}
