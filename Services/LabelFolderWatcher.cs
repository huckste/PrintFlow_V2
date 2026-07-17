namespace PrintFlow_V2.Services;

using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;
using Serilog;

public class LabelFolderWatcher
{
    private readonly FileSystemWatcher _labelDataLoadWatcher;
    private readonly FileSystemWatcher _labelsDirWatcher;
    private readonly PathSchema _pathSchema;

    public event Action<LabelFile>? FileCreated;
    public event Action<string>? FileDeleted;

    public LabelFolderWatcher(PathSchema pathSchema)
    {
        _pathSchema = pathSchema;

        _labelDataLoadWatcher = new FileSystemWatcher(pathSchema.LabelDataLoad.Path)
        {
            EnableRaisingEvents = true,
        };

        _labelsDirWatcher = new FileSystemWatcher(pathSchema.LabelsDir.Path)
        {
            EnableRaisingEvents = true,
        };

        _labelDataLoadWatcher.Created += OnFileAdded;
        _labelDataLoadWatcher.Deleted += OnFileRemoved;

        _labelsDirWatcher.Created += OnFileCreated;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        string ext = Path.GetExtension(e.FullPath).ToUpper();
        string dest = Path.Combine(_pathSchema.LabelDataLoad.Path, Path.GetFileName(e.FullPath));

        Log.Information("File {File} has been created", Path.GetFileName(e.FullPath));

        if (ext != ".PKL" && ext != ".SNGL")
            Safely.Copy(e.FullPath, dest).LogOnError();
    }

    private void OnFileAdded(object sender, FileSystemEventArgs e)
    {
        for (int i = 0; i < 5; i++)
        {
            var result = Safely
                .Run(
                    () => LabelService.BuildLabel(e.FullPath, _pathSchema),
                    Err.Action.Read,
                    e.FullPath
                )
                .LogOnError();

            if (!result.IsError)
            {
                FileCreated?.Invoke(result.Value);
                Log.Information(
                    "File {File} has been added from {Directory}",
                    Path.GetFileName(e.FullPath),
                    Path.GetDirectoryName(e.FullPath)
                );
                break;
            }

            Thread.Sleep(300);
        }
    }

    private void OnFileRemoved(object sender, FileSystemEventArgs e)
    {
        FileDeleted?.Invoke(e.FullPath);

        Log.Information(
            "File {File} has been removed from {Directory}",
            Path.GetFileName(e.FullPath),
            Path.GetDirectoryName(e.FullPath)
        );
    }
}
