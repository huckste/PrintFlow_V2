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
                        .Where(f => !Path.GetExtension(f).Equals(".SNGL"))
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
                Safely.Copy(file, destFile).CollectTo(errors);
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
                () =>
                    Directory
                        .GetFiles(pathSchema.LabelDataLoad.Path)
                        .Select(fp => BuildLabel(fp, pathSchema))
                        .ToList(),
                Err.Action.Read,
                pathSchema.LabelDataLoad.Path
            )
            .CollectTo(errors);

        return errors.Count > 0 ? errors : result.Value;
    }

    public static LabelFile BuildLabel(string filePath, PathSchema pathSchema)
    {
        bool isGtp = Path.GetFileName(filePath).Contains("GTP");

        using var reader = new StreamReader(filePath);

        string? firstLine = reader.ReadLine();
        string? secondLine = reader.ReadLine();
        string desc = firstLine?.Split('^')[8] ?? string.Empty;

        if (isGtp)
        {
            string? waveNumber = secondLine?.Split('^')[22].Trim();

            if (waveNumber != null)
            {
                string? newDesc = GetGtpDesc(waveNumber, pathSchema);

                if (newDesc != null)
                    desc = newDesc;
            }
        }

        int lineCount = 0;

        if (firstLine != null)
            lineCount++;

        if (secondLine != null)
            lineCount++;

        while (reader.ReadLine() != null)
            lineCount++;

        return new LabelFile(Path.GetFileName(filePath), filePath, desc, lineCount);
    }

    private static string? GetGtpDesc(string waveNumber, PathSchema pathSchema)
    {
        string? filePath = Directory
            .GetFiles(pathSchema.LabelDataLoad.Path)
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == waveNumber);

        filePath ??= Directory
            .GetFiles(pathSchema.LabelsDir.Path)
            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == waveNumber);

        if (filePath != null)
        {
            using var reader = new StreamReader(filePath);
            return reader.ReadLine()?.Split('^')[8];
        }

        return null;
    }

    // When a file is added to labelDataLoad there can be a race condition between trying to read the file as the file is still being written
    // Havaing attemprts allows for a retry incase there was a race condition
    public static LabelFile? TryBuildLabel(
        PathSchema pathSchema,
        string filePath,
        int retries = 5,
        int delayMs = 200
    )
    {
        for (int i = 0; i < retries; i++)
        {
            try
            {
                return BuildLabel(filePath, pathSchema);
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
            pathSchema.Archive.Path,
            Path.GetFileName(file.OriginalFilePath)
        );

        string originalPath = Path.Combine(
            pathSchema.LabelsDir.Path,
            Path.GetFileName(file.OriginalFilePath)
        );

        Safely.Run(() => File.Copy(originalPath, dest), Err.Action.Copy, dest).LogOnError();
    }
}
