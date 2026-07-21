using System.Runtime.CompilerServices;
using ErrorOr;

namespace PrintFlow_V2.Errors;

public static class Safely
{
    public static ErrorOr<T> Run<T>(
        Func<T> func,
        Err.Action actionType,
        string target,
        [CallerMemberName] string caller = ""
    )
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            return Err.FailedTo(actionType, target, ex.Message, caller);
        }
    }

    public static ErrorOr<Success> Copy(string sourceFile, string dest)
    {
        FileInfo file = new(sourceFile);
        Error error = new();

        for (int i = 0; i < 5; i++)
        {
            try
            {
                using (file.Open(FileMode.Open, FileAccess.Read, FileShare.None)) { }
                long fileSize = file.Length;

                while (true)
                {
                    Thread.Sleep(500);

                    file.Refresh();

                    if (fileSize == file.Length)
                    {
                        File.Copy(sourceFile, dest, overwrite: true);
                        return Result.Success;
                    }
                    else if (fileSize < file.Length)
                    {
                        fileSize = file.Length;
                    }
                    else
                    {
                        return Err.FailedTo(
                            Err.Action.Copy,
                            sourceFile,
                            "File size decreased during write"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                Thread.Sleep(1000);
                error = Err.FailedTo(Err.Action.Copy, sourceFile, ex.Message);
            }
        }

        return error;
    }

    public static ErrorOr<Success> Run(
        Action action,
        Err.Action actionType,
        string target,
        [CallerMemberName] string caller = ""
    )
    {
        try
        {
            action();
            return Result.Success;
        }
        catch (Exception ex)
        {
            return Err.FailedTo(actionType, target, ex.Message, caller);
        }
    }
}
