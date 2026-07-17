namespace PrintFlow_V2.Services;

using ErrorOr;
using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;

public class PrinterService()
{
    public static ErrorOr<List<Printer>> GetPrinters(PathSchema pathSchema)
    {
        List<Printer> allPrinters = [];
        var printers = pathSchema.PrintersDict();

        if (printers.IsError)
            return printers.Errors;

        int maxLen = printers.Value.Max(name => name.Key.Length);

        return printers
            .Value.Select(
                (kvp, i) =>
                {
                    return new Printer(kvp.Key, kvp.Value, maxLen);
                }
            )
            .ToList();
    }

    public static ErrorOr<Success> RemoveFromQueue(
        PathSchema pathSchema,
        PrintState state,
        List<LabelFile> files,
        Printer printer
    )
    {
        List<Error> errors = [];
        List<LabelFile> removedFiles = [];

        foreach (var file in files)
        {
            string dest = Path.Combine(
                pathSchema.LabelDataLoad.Path,
                Path.GetFileName(file.FilePath)
            );

            var moveFileResult = Safely
                .Run(() => File.Move(file.FilePath, dest), Err.Action.Move, file.FilePath)
                .CollectTo(errors);

            if (!moveFileResult.IsError)
            {
                removedFiles.Add(file);
                printer.UpdateFilePath(file, dest, Printer.PrinterQueue.Queued);
            }
        }

        state.AddFiles(printer.DequeueFiles(removedFiles, Printer.PrinterQueue.Queued));

        return errors.Count > 0 ? errors : Result.Success;
    }

    public static void AssignToPrinter(PrintState state, List<LabelFile> files, Printer printer)
    {
        state.RemoveFiles(files);
        printer.EnqueueFiles(files, Printer.PrinterQueue.Staged);
    }

    public static void RemoveFromStaged(PrintState state, List<LabelFile> files, Printer printer) =>
        state.AddFiles(printer.DequeueFiles(files, Printer.PrinterQueue.Staged));

    public static ErrorOr<List<LabelFile>> SendStagedFiles(Printer printer)
    {
        string[] fbcopExt = [".FBCOP", ".COP", ".FOP", ".BOP"];
        string[] popExt = [".POP", ".PCT"];

        string? printerPath = null;
        List<Error> errors = [];

        IReadOnlyList<LabelFile> staged = printer.QueueSnapshot(Printer.PrinterQueue.Staged);
        List<LabelFile> queuedFiles = [];

        foreach (LabelFile file in staged)
        {
            string ext = Path.GetExtension(file.FilePath).ToUpper();
            string destFileName = "";

            if (fbcopExt.Contains(ext))
            {
                destFileName = ext switch
                {
                    ".FBCOP" => file.FilePath,
                    _ => $"{file.FilePath}.FBCOP",
                };

                printerPath ??= printer.CopPath;
            }
            else if (popExt.Contains(ext))
            {
                destFileName = ext switch
                {
                    ".POP" => file.FilePath,
                    _ => $"{file.FilePath}.POP",
                };

                printerPath = printer.PopPath;
            }
            else if (ext == ".PKL")
            {
                destFileName = file.FilePath;
                printerPath = printer.PklPath;
            }

            if (printerPath is null)
                return errors;

            string dest = Path.Combine(printerPath, Path.GetFileName(destFileName));
            string fromPath = file.FilePath;

            printer.UpdateFilePath(file, dest, Printer.PrinterQueue.Staged);
            printer.MoveQueue(Printer.PrinterQueue.Staged, Printer.PrinterQueue.Queued, file);

            var moveResult = Safely
                .Run(() => File.Move(fromPath, dest), Err.Action.Move, fromPath)
                .CollectTo(errors);

            if (moveResult.IsError)
            {
                printer.UpdateFilePath(file, fromPath, Printer.PrinterQueue.Queued);
                printer.MoveQueue(Printer.PrinterQueue.Queued, Printer.PrinterQueue.Staged, file);
            }
            else
            {
                queuedFiles.Add(file);
            }
        }

        return errors.Count > 0 ? errors : queuedFiles;
    }
}
