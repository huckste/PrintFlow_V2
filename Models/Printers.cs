namespace PrintFlow_V2.Models;

using Serilog;

public class Printer
{
    private readonly Lock _lock = new();

    private readonly FileSystemWatcher? _popWatcher;
    private readonly FileSystemWatcher? _copWatcher;
    private readonly FileSystemWatcher? _pklWatcher;

    private readonly List<LabelFile> _queued = [];
    private readonly List<LabelFile> _staged = [];
    private readonly List<LabelFile> _active = [];

    public string Name { get; }
    public string? PopPath { get; }
    public string? CopPath { get; }
    public string? PklPath { get; }
    public int MaxLen { get; }

    public event Action<LabelFile>? FileCompleted;

    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];

    public enum PrinterQueue
    {
        Staged,
        Queued,
        Active,
    }

    public enum PrinterType
    {
        COP,
        POP,
        PKL,
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

    public Printer(string name, Dictionary<PrinterType, string> printerPaths, int maxLen)
    {
        Name = name;
        MaxLen = maxLen;

        foreach (var (key, value) in printerPaths)
        {
            switch (key)
            {
                case PrinterType.COP:
                    CopPath = value;
                    SetupWatcher(ref _copWatcher, CopPath);

                    break;
                case PrinterType.POP:
                    PopPath = value;
                    SetupWatcher(ref _popWatcher, PopPath);

                    break;
                case PrinterType.PKL:
                    PklPath = value;
                    SetupWatcher(ref _pklWatcher, PklPath);

                    break;
            }
        }
    }

    private void SetupWatcher(ref FileSystemWatcher? fsw, string path)
    {
        fsw = new FileSystemWatcher(path)
        {
            EnableRaisingEvents = true,
            InternalBufferSize = 65536,
            NotifyFilter = NotifyFilters.FileName,
        };

        fsw.Deleted += OnFileDeleted;
        fsw.Renamed += OnFileRenamed;
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
                    Log.Information("Removed from queued: {File}", label.FileName);

                    label.FilePath = e.FullPath;

                    _active.Add(label);
                    Log.Information("Added to active: {File}", label.FileName);
                }
            }
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        lock (_lock)
        {
            LabelFile? active = _active.FirstOrDefault(l =>
                l.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase)
            );

            if (active != null)
            {
                _active.RemoveAll(l => l.Id == active.Id);
                Log.Information("File Completed: {FileName}", active.FileName);

                // when file is completed allow external code to react and archive the file
                FileCompleted?.Invoke(active);
            }

            // If a file is deleted not due to completing or from a program operation then clear it from queue or else it will be stuck
            LabelFile? queued = _queued.FirstOrDefault(qf =>
                qf.FilePath.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase)
            );

            if (queued != null)
                _queued.RemoveAll(qf => qf.Id == queued.Id);
        }
    }

    public void EnqueueFiles(List<LabelFile> files, PrinterQueue queue)
    {
        lock (_lock)
        {
            List<LabelFile> target = GetTargetQueue(queue);
            target.AddRange(files.Where(f => !target.Any(tf => tf.Id == f.Id)));

            foreach (var file in files)
                Log.Information("File {File} added to {Queue}", file.FileName, queue);
        }
    }

    public List<LabelFile> DequeueFiles(List<LabelFile> files, PrinterQueue queue)
    {
        lock (_lock)
        {
            List<LabelFile> target = GetTargetQueue(queue);
            List<LabelFile> removed = [.. target.Where(tf => files.Any(f => f.Id == tf.Id))];

            target.RemoveAll(tf => files.Any(f => f.Id == tf.Id));

            foreach (var file in files)
                Log.Information("File {File} removed from {Queue}", file.FileName, queue);

            return removed;
        }
    }

    public void ClearQueue(PrinterQueue queue)
    {
        lock (_lock)
        {
            GetTargetQueue(queue).Clear();
            Log.Information("Queue {Queue} has been cleared", queue);
        }
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

            foreach (var file in files)
                Log.Information(
                    "File {File} removed from {FromQueue} and added to {ToQueue}",
                    file.FileName,
                    fromQueue,
                    toQueue
                );
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

                Log.Information(
                    "File {File} removed from {FromQueue} and added to {ToQueue}",
                    file.FileName,
                    fromQueue,
                    toQueue
                );
            }
        }
    }

    public void UpdateFilePath(LabelFile file, string newPath, PrinterQueue queue)
    {
        lock (_lock)
        {
            LabelFile? labelFile = GetTargetQueue(queue).FirstOrDefault(f => f.Id == file.Id);
            string? currentPath = labelFile?.FilePath;

            labelFile?.FilePath = newPath;

            Log.Information(
                "File {File} in {Queue} has changed path from {CurrentPath} to {NewPath}",
                file.FileName,
                queue,
                currentPath,
                newPath
            );
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

    public int? GetAllQueueCount()
    {
        int total = 0;

        foreach (var queue in Enum.GetValues<PrinterQueue>())
            total += GetQueueCount(queue);

        return total == 0 ? null : total;
    }

    public int GetMaxFileNameAllQueues()
    {
        int total = 0;

        foreach (var queue in Enum.GetValues<PrinterQueue>())
            total += MaxFileNameLength(queue);

        return total;
    }

    public int MaxFileNameLength(PrinterQueue queue)
    {
        lock (_lock)
        {
            List<LabelFile> target = GetTargetQueue(queue);
            return target.Count > 0 ? target.Max(f => f.FileName.Length) : 0;
        }
    }

    public IReadOnlyList<LabelFile> QueueSnapshot(PrinterQueue queue)
    {
        lock (_lock)
            return [.. GetTargetQueue(queue)];
    }
}
