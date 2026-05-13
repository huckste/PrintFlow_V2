namespace PrintFlow_V2.Models;

public class Printer
{
    public string Name { get; }
    public string FullName { get; }
    public int MaxLen { get; }
    public int Active { get; } = 0;
    public int Waiting { get; } = 0;
    public List<LabelFile> Queue { get; } = [];

    public Printer(string name, string fullName, int maxLen)
    {
        Name = name;
        FullName = fullName;
        MaxLen = maxLen;
    }

    public int TotalLabels => Queue.Sum(f => f.LabelCount);
    public string PadName => Name.PadRight(MaxLen);
}
