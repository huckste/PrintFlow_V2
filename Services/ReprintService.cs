namespace PrintFlow_V2.Services;

using PrintFlow_V2.Models;

public class ReprintService
{
    private const int WaveSeqNum = 13;
    private const string HeaderType = "1";
    private const int TypeField = 5;

    private static void WriteReprintFile(
        string[] lines,
        string filePath,
        int startLine,
        int endLine
    )
    {
        string outputDir = @"C:/Label_Data_Load";
        string waveNumber = Path.GetFileName(filePath).Split('_', '.')[0];
        string suffix = Path.GetFileName(filePath)[waveNumber.Length..];
        string reprintLabel =
            $"^^^^^1^Reprint^^^^^^^^^^^^^{waveNumber}^^{DateOnly.FromDateTime(DateTime.Now)}^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^";

        int startPos = (startLine - 1) % 4 + 1;
        int endPos = (endLine - 1) % 4 + 1;
        int prependCount = (startPos == 1) ? 4 : startPos - 1;
        int appendCount = (endPos == 4) ? 4 : 4 - endPos;

        string[] result =
        [
            .. Enumerable.Repeat(reprintLabel, prependCount),
            .. lines[(startLine - 1)..endLine],
            .. Enumerable.Repeat(reprintLabel, appendCount),
        ];

        string newFileName = $"{waveNumber}({startLine}-{endLine}){suffix}";
        File.WriteAllLines(Path.Combine(outputDir, newFileName), result);
    }

    public static ReprintContext ReprintByHeader(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);

        Dictionary<string, int> lookup = lines
            .Select((line, i) => (line, fields: line.Split('^'), i: i + 1))
            .Where(x => x.fields[TypeField] == HeaderType)
            .ToDictionary(x => x.fields[WaveSeqNum], x => x.i);

        return new ReprintContext(lines, lookup, filePath);
    }

    public static void ReprintByLine(string filePath, int startLine, int endLine)
    {
        string[] lines = File.ReadAllLines(filePath);
        WriteReprintFile(lines, filePath, startLine, endLine);
    }
}
