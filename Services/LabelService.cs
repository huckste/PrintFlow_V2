namespace PrintFlow_V2.Services;

using ErrorOr;
using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;

public class LabelService
{
    public static void CopyFiles(PathSchema pathSchema)
    {
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
            .LogOnError();

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
            .LogOnError();

        string[] filesNotPrinted =
        [
            .. todaysFiles.Value.Where(f => !archiveFiles.Value.Contains(Path.GetFileName(f))),
        ];

        foreach (var file in filesNotPrinted)
        {
            var destFile = Path.Combine(pathSchema.LabelDataLoad.Path, Path.GetFileName(file));
            Safely.Copy(file, destFile).LogOnError();
        }
    }

    public static ErrorOr<List<LabelFile>> GetLabels(PathSchema pathSchema)
    {
        CopyFiles(pathSchema);

        return Safely
            .Run(
                () =>
                    Directory
                        .GetFiles(pathSchema.LabelDataLoad.Path)
                        .Select(fp => BuildLabel(fp, pathSchema))
                        .ToList(),
                Err.Action.Read,
                pathSchema.LabelDataLoad.Path
            )
            .LogOnError();
    }

    public static LabelFile BuildLabel(string filePath, PathSchema pathSchema)
    {
        using var reader = new StreamReader(filePath);

        bool isGtp = Path.GetFileName(filePath).Contains("GTP");
        string desc = reader.ReadLine()?.Split('^')[8] ?? string.Empty;

        if (isGtp)
        {
            string? waveNumber = reader.ReadLine()?.Split('^')[22].Trim();

            if (waveNumber != null)
            {
                string? newDesc = GetGtpDesc(waveNumber, pathSchema);

                if (newDesc != null)
                    desc = newDesc;
            }
        }

        return new LabelFile(Path.GetFileName(filePath), filePath, desc, GetLineCount(filePath));
    }

    public static int GetLineCount(string path)
    {
        int count = 0;
        using var stream = File.OpenRead(path);

        Span<byte> buffer = stackalloc byte[4096];
        int read;

        while ((read = stream.Read(buffer)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                if (buffer[i] == '\n')
                    count++;
            }
        }

        return count + 1; // last line with no trailling newline
    }

    private static string? GetGtpDesc(string waveNumber, PathSchema pathSchema)
    {
        var result = Safely
            .Run(
                () =>
                    Directory
                        .GetFiles(pathSchema.LabelDataLoad.Path)
                        .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == waveNumber),
                Err.Action.Read,
                pathSchema.LabelDataLoad.Path
            )
            .LogOnError();

        if (result.IsError || result.Value is null)
        {
            result = Safely
                .Run(
                    () =>
                        Directory
                            .GetFiles(pathSchema.LabelsDir.Path)
                            .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == waveNumber),
                    Err.Action.Read,
                    pathSchema.LabelsDir.Path
                )
                .LogOnError();
        }

        if (result.IsError || result.Value is null)
            return null;

        using var reader = new StreamReader(result.Value);
        return reader.ReadLine()?.Split('^')[8];
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

        Safely
            .Run(() => File.Copy(originalPath, dest, overwrite: true), Err.Action.Copy, dest)
            .LogOnError();
    }
}
