using System.Runtime.CompilerServices;
using ErrorOr;

namespace PrintFlow_V2.Errors;

public static class Err
{
    public static Error NotFound(
        NotFoundType type,
        string item,
        string? reason = null,
        [CallerMemberName] string caller = ""
    )
    {
        return Error.NotFound(
            $"{caller}.{type}NotFound",
            reason is null
                ? $"[[Could not find {type}]] '{item}'"
                : $"[[Could not find {type}]] '{item}' => {reason}"
        );
    }

    public static Error FailedTo(
        Action action,
        string item,
        string? reason = null,
        [CallerMemberName] string caller = ""
    )
    {
        return Error.Failure(
            $"{caller}.FailedTo{action}",
            reason is null
                ? $"[[Failed to {action}]] '{item}'"
                : $"[[Failed to {action}]] '{item}' => {reason}"
        );
    }

    public enum Action
    {
        Move,
        Delete,
        Create,
        Open,
        Archive,
        Deserialize,
        Load,
        Save,
        Validate,
        Read,
        Write,
        Copy,
        Find,
        Complete,
    }

    public enum NotFoundType
    {
        Directory,
        File,
    }
}
