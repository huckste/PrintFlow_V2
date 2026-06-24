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
