namespace PrintFlow_V2.Models;

public class ReprintContext(string[] lines, Dictionary<string, int> headers, string filePath)
{
    public string[] Lines { get; } = lines;
    public string FilePath { get; } = filePath;
    public Dictionary<string, int> Headers { get; } = headers;

    public bool Lookup(string headerNumber) => Headers.TryGetValue(headerNumber, out _);
}
