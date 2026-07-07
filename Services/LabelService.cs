namespace PrintFlow_V2.Services;

using ErrorOr;
using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;

public class LabelService
{
    public static ErrorOr<Success> CopyFiles(PathSchema pathSchema)
    {
        List<Error> errors = [];

        var todaysFiles = Safely
            .Run(
                () =>
                {
                    return Directory
                        .GetFiles(pathSchema.LabelsDir.Path)
                        .Where(f => File.GetCreationTime(f).Date == DateTime.Today)
                        .Where(f =>
                            !Path.GetExtension(f).Equals(".SNGL")
                            && !Path.GetExtension(f).Equals(".PKL")
                        )
                        .ToList();
                },
                Err.Action.Read,
                pathSchema.LabelsDir.Path
            )
            .CollectTo(errors);

        var now = DateTime.Now;

        var archiveFiles = Safely
            .Run(
                () =>
                {
                    return Directory
                        .GetFiles(pathSchema.Archive.Path)
                        .Where(f =>
                        {
                            var info = new FileInfo(f);
                            return info.LastWriteTime.Month == now.Month
                                && info.LastWriteTime.Year == now.Year;
                        })
                        .Select(f => Path.GetFileName(f))
                        .ToList();
                },
                Err.Action.Read,
                pathSchema.Archive.Path
            )
            .CollectTo(errors);

        if (!todaysFiles.IsError && !archiveFiles.IsError)
        {
            string[] filesNotPrinted =
            [
                .. todaysFiles.Value.Where(f => !archiveFiles.Value.Contains(Path.GetFileName(f))),
            ];

            foreach (var file in filesNotPrinted)
            {
                var destFile = Path.Combine(pathSchema.LabelDataLoad.Path, Path.GetFileName(file));

                Safely
                    .Run(
                        () => File.Copy(file, destFile, overwrite: true),
                        Err.Action.Copy,
                        destFile
                    )
                    .CollectTo(errors);
            }
        }

        return errors.Count > 0 ? errors : Result.Success;
    }

    public static ErrorOr<List<LabelFile>> GetLabels(PathSchema pathSchema)
    {
        List<Error> errors = [];
        CopyFiles(pathSchema).CollectTo(errors);

        var result = Safely
            .Run(
                () => Directory.GetFiles(pathSchema.LabelDataLoad.Path).Select(BuildLabel).ToList(),
                Err.Action.Read,
                pathSchema.LabelDataLoad.Path
            )
            .CollectTo(errors);

        return errors.Count > 0 ? errors : result.Value;
    }

    public static LabelFile BuildLabel(string filePath)
    {
        using var reader = new StreamReader(filePath);
        string? firstLine = reader.ReadLine();
        string desc = firstLine?.Split('^')[8] ?? string.Empty;
        int lineCount = 0;

        if (firstLine != null)
            lineCount++;

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

    public static void ArchiveFile(LabelFile file, PathSchema pathSchema)
    {
        string dest = Path.Combine(
            Path.GetFileName(file.OriginalFilePath),
            pathSchema.Archive.Path
        );

        Safely.Run(() => File.Copy(file.OriginalFilePath, dest), Err.Action.Copy, dest);
    }
}
