namespace PrintFlow_V2.Services;

using PrintFlow_V2.Models;

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

    // Trying to see if this can be used by other classes to get files that are Queued or Active

    public static List<LabelFile> GetLabels(string? path = null)
    {
        string dir = path ?? _labelDataLoad;

        if (!Directory.EnumerateFiles(_labelDataLoad).Any() && path == null)
            CopyFiles();

        return
        [
            .. Directory
                .GetFiles(dir)
                .Select(filePath =>
                {
                    using var reader = new StreamReader(filePath);
                    string? firstLine = reader.ReadLine();
                    string desc = firstLine?.Split('^')[8] ?? string.Empty;
                    int lineCount = 1;

                    while (reader.ReadLine() != null)
                        lineCount++;

                    return new LabelFile(Path.GetFileName(filePath), filePath, desc, lineCount);
                }),
        ];
    }
}
