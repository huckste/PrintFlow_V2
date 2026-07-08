namespace PrintFlow_V2.Services;

using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;

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

        if (ext != ".PKL" && ext != ".SNGL")
            Safely.Run(() => File.Copy(e.FullPath, dest), Err.Action.Copy, dest);
    }

    private void OnFileAdded(object sender, FileSystemEventArgs e)
    {
        LabelFile? label = LabelService.TryBuildLabel(_pathSchema, e.FullPath);

        if (label == null)
            return;

        FileCreated?.Invoke(label);
    }

    private void OnFileRemoved(object sender, FileSystemEventArgs e)
    {
        FileDeleted?.Invoke(e.FullPath);
    }
}
