namespace PrintFlow_V2.Models;

using ErrorOr;
using PrintFlow_V2.UI;

public class Printer
{
    private readonly Lock _lock = new();
    private readonly FileSystemWatcher _popWatcher;
    private readonly FileSystemWatcher _copWatcher;

    private readonly List<LabelFile> _queued = [];
    private readonly List<LabelFile> _staged = [];
    private readonly List<LabelFile> _active = [];

    public string Name { get; }
    public string PopPath { get; }
    public string CopPath { get; }
    public int MaxLen { get; }

    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    public enum PrinterQueue
    {
        Active,
        Queued,
        Staged,
    }

    public int TotalLabels
    {
        get
        {
            lock (_lock)
            {
                return _staged.Sum(f => f.LabelCount)
                    + _queued.Sum(f => f.LabelCount)
                    + _active.Sum(f => f.LabelCount);
            }
        }
    }

    public string PadName => Name.PadRight(MaxLen);

    public Printer(string name, string copPath, string popPath, int maxLen)
    {
        Name = name;
        CopPath = copPath;
        PopPath = popPath;
        MaxLen = maxLen;

        _copWatcher = new FileSystemWatcher(copPath)
        {
            EnableRaisingEvents = true,
            InternalBufferSize = 65536,
            NotifyFilter = NotifyFilters.FileName,
        };
        _popWatcher = new FileSystemWatcher(popPath)
        {
            EnableRaisingEvents = true,
            InternalBufferSize = 65536,
            NotifyFilter = NotifyFilters.FileName,
        };

        _copWatcher.Deleted += OnFileDeleted;
        _popWatcher.Deleted += OnFileDeleted;

        _copWatcher.Renamed += OnFileRenamed;
        _popWatcher.Renamed += OnFileRenamed;
    }

    private List<LabelFile> GetTargetQueue(PrinterQueue queue) =>
        queue switch
        {
            PrinterQueue.Active => _active,
            PrinterQueue.Staged => _staged,
            PrinterQueue.Queued => _queued,
            _ => throw new ArgumentOutOfRangeException(nameof(queue)),
        };

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        string ext = Path.GetExtension(e.FullPath).ToUpper();

        if (ext == ".PROCESSED")
        {
            lock (_lock)
            {
                LabelFile? label = _queued.FirstOrDefault(l =>
                    l.FilePath.Equals(e.OldFullPath, StringComparison.OrdinalIgnoreCase)
                );

                if (label != null)
                {
                    _queued.RemoveAll(l => l.Id == label.Id);
                    label.FilePath = e.FullPath;
                    _active.Add(label);
                }

                File.AppendAllLines(
                    @"C:\Temp\watcher.log",
                    [
                        $"{DateTime.Now}: Renamed: {e.OldFullPath} -> {e.FullPath}",
                        $"{DateTime.Now}: Queued: {string.Join(", ", _queued.Select(l =>
                        l.FilePath))}",
                    ]
                );
            }
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            LabelFile? label = _active.FirstOrDefault(l =>
                l.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase)
            );

            if (label != null)
                _active.RemoveAll(l => l.Id == label.Id);

            File.AppendAllLines(
                @"C:\Temp\watcher.log",
                [
                    $"{DateTime.Now}: Deleted: {e.FullPath}",
                    $"{DateTime.Now}: Active: {string.Join(", ", _active.Select(l =>
                    l.FilePath))}",
                ]
            );
        }
    }

    public void EnqueueFiles(List<LabelFile> files, PrinterQueue queue)
    {
        lock (_lock)
        {
            List<LabelFile> target = GetTargetQueue(queue);
            target.AddRange(files.Where(f => !target.Any(tf => tf.Id == f.Id)));
        }
    }

    public List<LabelFile> DequeueFiles(List<LabelFile> files, PrinterQueue queue)
    {
        lock (_lock)
        {
            List<LabelFile> target = GetTargetQueue(queue);
            List<LabelFile> removed = [.. target.Where(tf => files.Any(f => f.Id == tf.Id))];

            target.RemoveAll(tf => files.Any(f => f.Id == tf.Id));
            return removed;
        }
    }

    public void ClearQueue(PrinterQueue queue)
    {
        lock (_lock)
            GetTargetQueue(queue).Clear();
    }

    public void MoveQueue(PrinterQueue fromQueue, PrinterQueue toQueue, List<LabelFile> files)
    {
        lock (_lock)
        {
            var fromTarget = GetTargetQueue(fromQueue);
            var toTarget = GetTargetQueue(toQueue);

            var toMove = fromTarget.Where(tf => files.Any(f => f.Id == tf.Id)).ToList();

            toTarget.AddRange(toMove.Where(f => !toTarget.Any(t => t.Id == f.Id)));
            fromTarget.RemoveAll(tf => toMove.Any(f => f.Id == tf.Id));
        }
    }

    public void MoveQueue(PrinterQueue fromQueue, PrinterQueue toQueue, LabelFile file)
    {
        lock (_lock)
        {
            var fromTarget = GetTargetQueue(fromQueue);
            var toTarget = GetTargetQueue(toQueue);

            // Need to grab the original object vs using the snapshot version
            var toMove = fromTarget.FirstOrDefault(ft => ft.Id == file.Id);

            if (toMove != null && !toTarget.Any(tt => tt.Id == toMove.Id))
            {
                toTarget.Add(toMove);
                fromTarget.Remove(toMove);
            }
        }
    }

    public void UpdateFilePath(LabelFile file, string newPath, PrinterQueue queue)
    {
        lock (_lock)
        {
            LabelFile? labelFile = GetTargetQueue(queue).FirstOrDefault(f => f.Id == file.Id);
            labelFile?.FilePath = newPath;
        }
    }

    public int GetQueueCount(PrinterQueue queue)
    {
        lock (_lock)
        {
            List<LabelFile> target = GetTargetQueue(queue);
            return target.Count;
        }
    }

    public int MaxFileNameLength(PrinterQueue queue)
    {
        lock (_lock)
        {
            List<LabelFile> target = GetTargetQueue(queue);
            return target.Max(f => f.FileName.Length);
        }
    }

    public IReadOnlyList<LabelFile> QueueSnapshot(PrinterQueue queue)
    {
        lock (_lock)
            return [.. GetTargetQueue(queue)];
    }
}
