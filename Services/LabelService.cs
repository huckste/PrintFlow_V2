namespace PrintFlow_V2.Services;

using PrintFlow_V2.Config;
using PrintFlow_V2.Models;

public class LabelService
{
    public static void CopyFiles(PathSchema pathSchema)
    {
        string[] todaysFiles =
        [
            .. Directory
                .GetFiles(pathSchema.LabelsDir.Path)
                .Where(f => File.GetCreationTime(f).Date == DateTime.Today)
                .Where(f =>
                    !Path.GetExtension(f).Equals(".SNGL") && !Path.GetExtension(f).Equals(".PKL")
                ),
        ];

        var now = DateTime.Now;

        string[] archiveFiles =
        [
            .. Directory
                .GetFiles(pathSchema.Archive.Path)
                .Where(f =>
                {
                    var info = new FileInfo(f);
                    return info.LastWriteTime.Month == now.Month
                        && info.LastWriteTime.Year == now.Year;
                })
                .Select(f => Path.GetFileName(f)),
        ];

        string[] filesNotPrinted =
        [
            .. todaysFiles.Where(f => !archiveFiles.Contains(Path.GetFileName(f))),
        ];

        foreach (var file in filesNotPrinted)
        {
            var destFile = Path.Combine(pathSchema.LabelDataLoad.Path, Path.GetFileName(file));
            File.Copy(file, destFile, overwrite: true);
        }
    }

    public static List<LabelFile> GetLabels(PathSchema pathSchema)
    {
        CopyFiles(pathSchema);
        return [.. Directory.GetFiles(pathSchema.LabelDataLoad.Path).Select(BuildLabel)];
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
