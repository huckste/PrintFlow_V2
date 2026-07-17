using ErrorOr;
using PrintFlow_V2.UI;
using Serilog;

namespace PrintFlow_V2.Errors;

public static class ErrorOrExtensions
{
    public static ErrorOr<T> DisplayOnError<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
            Messages.Error(result.Errors);

        return result;
    }

    public static ErrorOr<T> LogOnError<T>(this ErrorOr<T> result)
    {
        if (result.IsError)
        {
            foreach (var error in result.Errors)
                Log.Error("[{Code}] {Description}", error.Code, error.Description);
        }

        return result;
    }

    public static void LogToFile(this List<Error> errors)
    {
        foreach (var e in errors)
            Log.Error("[{Code}] {Description}", e.Code, e.Description);
    }

    public static List<T> SuccessOrLog<T>(this IEnumerable<ErrorOr<T>> results)
    {
        var values = new List<T>();
        var errors = new List<Error>();

        foreach (var r in results)
        {
            if (r.IsError)
                errors.AddRange(r.Errors);
            else
                values.Add(r.Value);
        }

        errors.LogToFile();
        return values;
    }

    public static ErrorOr<T> CollectTo<T>(this ErrorOr<T> result, List<Error> sink)
    {
        if (result.IsError)
            sink.AddRange(result.Errors);

        return result;
    }

    public static ErrorOr<Success> Discard<T>(this ErrorOr<T> r) =>
        r.IsError ? r.Errors : Result.Success;
}
