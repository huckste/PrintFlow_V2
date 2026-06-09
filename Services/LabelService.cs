namespace PrintFlow_V2.Services;

using PrintFlow_V2.Models;
using PrintFlow_V2.UI;

public class LabelService
{
    private static readonly string _labelDir = @"\\ind-as84\asroot$\labels";
    private static readonly string _labelDataLoad = @"C:\Temp\Label_Data_Load";

    public static void CopyFiles()
    {
        string[] files =
        [
            .. Directory
                .GetFiles(_labelDir)
                .Where(f => File.GetCreationTime(f).Date == DateTime.Today)
                .Where(f =>
                    !Path.GetExtension(f).Equals(".SNGL") && !Path.GetExtension(f).Equals(".PKL")
                ),
        ];

        foreach (var file in files)
        {
            var destFile = Path.Combine(_labelDataLoad, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }
    }

    public static List<LabelFile> GetLabels(string? path = null)
    {
        string dir = path ?? _labelDataLoad;

        if (!Directory.EnumerateFiles(_labelDataLoad).Any() && path == null)
            CopyFiles();

        return [.. Directory.GetFiles(dir).Select(BuildLabel)];
    }

    public static LabelFile BuildLabel(string filePath)
    {
        using var reader = new StreamReader(filePath);
        string? firstLine = reader.ReadLine();
        string desc = firstLine?.Split('^')[8] ?? string.Empty;
        int lineCount = 1;

        while (reader.ReadLine() != null)
            lineCount++;

        return new LabelFile(Path.GetFileName(filePath), filePath, desc, lineCount);
    }

    // When a file is added to labelDataLoad there can be a race condition between trying to read the file as the file is still being written
    // Havaing attemprts allows for a retry incase there was a race condition
    public static LabelFile? TryBuildLabel(string filePath, int retries = 5, int delayMs = 200)
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                return BuildLabel(filePath);
            }
            catch (IOException)
            {
                Thread.Sleep(delayMs);
            }
        }

        return null;
    }
}
