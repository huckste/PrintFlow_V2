namespace PrintFlow_V2.Services;

using PrintFlow_V2.Models;

public class LabelService
{
    private static readonly string _labelDir = @"\\ind-as84\asroot$\labels";

    public static List<LabelFile> GetLabels()
    {
        List<string> todaysLabels =
        [
            .. Directory
                .GetFiles(_labelDir)
                .Where(f => File.GetCreationTime(f).Date == DateTime.Today)
                .Where(f =>
                    !Path.GetExtension(f).Equals(".SNGL") && !Path.GetExtension(f).Equals(".PKL")
                ),
        ];

        List<LabelFile> labels = [];

        foreach (string path in todaysLabels)
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
