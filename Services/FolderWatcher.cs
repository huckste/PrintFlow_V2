namespace PrintFlow_V2.Services;

using System.Collections.Concurrent;
using PrintFlow_V2.Models;

public class FolderWatcher
{
    private readonly ConcurrentDictionary<string, LabelFile> _cache = [];
    private readonly FileSystemWatcher _watcher;

    public event Action<LabelFile>? FileCreated;
    public event Action<LabelFile>? FileDeleted;

    public FolderWatcher(string path)
    {
        _watcher = new FileSystemWatcher(path) { EnableRaisingEvents = true };
        _watcher.Created += OnFileCreated;
        _watcher.Deleted += OnFileDeleted;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        LabelFile label = LabelService.BuildLabel(e.FullPath);
        _cache.TryAdd(label.FilePath, label);
        FileCreated?.Invoke(label);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (_cache.TryGetValue(e.FullPath, out var label))
            FileDeleted?.Invoke(label);
    }
}
