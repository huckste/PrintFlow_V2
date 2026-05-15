namespace PrintFlow_V2.Services;

using PrintFlow_V2.Models;

public class LabelService
{
    private static readonly string _labelDir = @"\\ind-as84\asroot$\labels";
    private static readonly string _labelDataLoad = @"C:\Temp\Label_Data_Load";

    public static void CopyFiles()
    {
        List<string> files =
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

    public static List<LabelFile> GetLabels()
    {
        bool hasFiles = Directory.EnumerateFiles(_labelDataLoad).Any();

        List<string> files = [];

        if (!hasFiles)
        {
            CopyFiles();

            files =
            [
                .. Directory
                    .GetFiles(_labelDir)
                    .Where(f => File.GetCreationTime(f).Date == DateTime.Today)
                    .Where(f =>
                        !Path.GetExtension(f).Equals(".SNGL")
                        && !Path.GetExtension(f).Equals(".PKL")
                    ),
            ];
        }
        else
        {
            files = [.. Directory.GetFiles(_labelDataLoad)];
        }

        List<LabelFile> labels = [];

        foreach (string path in files)
        {
            using var reader = new StreamReader(path);
            string? firstLine = reader.ReadLine();
            string desc = string.Empty;

            if (firstLine != null)
                desc = firstLine.Split('^')[8];

            string fileName = Path.GetFileName(path);

            int lineCount = 1;

            while (reader.ReadLine() != null)
                lineCount++;

            LabelFile label = new(fileName, desc, lineCount);

            labels.Add(label);
        }

        return labels;
    }
}
