namespace PrintFlow_V2.Services;

public class SearchService
{
    private const int _labelTypeField = 5;
    private static readonly Dictionary<WaveType, string[]> _waveTypes = new()
    {
        [WaveType.FBCOP] = [".FBCOP", ".COP", ".FOP", ".BOP"],
        [WaveType.POP] = [".PCT", ".POP", ".GTP"],
        [WaveType.SNGL] = [".SNGL"],
        [WaveType.PKL] = [".PKL"],
    };
    private static readonly Dictionary<string, string> _outputTypes = new()
    {
        [".FBCOP"] = "FBCOP",
        [".COP"] = "FBCOP",
        [".FOP"] = "FBCOP",
        [".BOP"] = "FBCOP",
        [".POP"] = "POP",
        [".PCT"] = "POP",
        [".GTP"] = "POP",
        [".SNGL"] = "POP",
        [".PKL"] = "PKL",
    };

    public static bool FindLabels(
        string searchValue,
        WaveType? waveType,
        string outPutDir,
        string searchDir,
        string? labelType = null,
        int? field = null,
        int? limit = null
    )
    {
        string[] allFiles = Directory.GetFiles(searchDir);

        var filtered =
            waveType == null
                ? allFiles
                : allFiles.Where(f => _waveTypes[waveType.Value].Contains(Path.GetExtension(f)));

        string[] files = [.. filtered.OrderByDescending(File.GetLastWriteTime)];

        Dictionary<string, List<string>> foundLabels = [];
        foundLabels.Add("FBCOP", []);
        foundLabels.Add("POP", []);
        foundLabels.Add("PKL", []);

        int foundCount = 0;

        foreach (string filePath in files)
        {
            if (limit != null && foundCount >= limit.Value)
                break;

            string fileExt = Path.GetExtension(filePath);
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var values = line.Split("^");

                if (
                    labelType != null
                    && waveType != WaveType.PKL
                    && values[_labelTypeField] != labelType
                )
                    continue;

                if (field != null)
                {
                    if (values[field.Value - 1] != searchValue)
                        continue;

                    foundLabels[_outputTypes[fileExt]].Add(line);
                    foundCount++;
                    continue;
                }

                if (!values.Any(v => v.Contains(searchValue)))
                    continue;

                if (limit != null && foundCount >= limit.Value)
                    break;

                foundLabels[_outputTypes[fileExt]].Add(line);
                foundCount++;
            }
        }

        foreach (var (key, lines) in foundLabels)
        {
            string destination = Path.Combine(outPutDir, $"FoundLabels.{key}");

            if (lines.Count > 0)
                File.WriteAllLines(destination, lines);
        }

        return foundCount > 0;
    }

    public enum WaveType
    {
        FBCOP,
        POP,
        SNGL,
        PKL,
    }
}
