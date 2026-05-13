namespace PrintFlow_V2.Models;

public class Printer(string name, string fullName, int maxLen)
{
    public string Name { get; } = name;
    public string FullName { get; } = fullName;
    public int MaxLen { get; } = maxLen;
    public int Active { get; } = 0;
    public int Waiting { get; } = 0;
    public List<LabelFile> Queue { get; } = [];

    public int TotalLabels => Queue.Sum(f => f.LabelCount);
    public string PadName => Name.PadRight(MaxLen);
}
