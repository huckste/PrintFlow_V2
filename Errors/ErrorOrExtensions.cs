using ErrorOr;
using PrintFlow_V2.UI;

namespace PrintFlow_V2.Errors;

public static class ErrorOrExtensions
{
    public static ErrorOr<T> LogOnError<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            Messages.Error(result.Errors);
            return result.Errors;
        }

        return result.Value;
    }

    public static ErrorOr<T> Collect<T>(this ErrorOr<T> result, List<Error> sink)
    {
        if (result.IsError)
            sink.AddRange(result.Errors);

        return result;
    }

    public static ErrorOr<Success> Discard<T>(this ErrorOr<T> r) =>
        r.IsError ? r.Errors : Result.Success;
}
