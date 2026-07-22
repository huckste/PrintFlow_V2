namespace PrintFlow_V2.UI;

using ErrorOr;
using Terminal.Gui.Views;

public static class Messages
{
    public static void Success(string message, string title = "Success") =>
        MessageBox.Query(TuiApp.App, title, message, "OK");

    public static void Warning(string message, string title = "Warning") =>
        MessageBox.Query(TuiApp.App, title, message, "OK");

    public static void Warning(Error error) =>
        Warning(error.Description);

    public static void Error(string message, string title = "Error") =>
        MessageBox.ErrorQuery(TuiApp.App, title, message, "OK");

    public static void Error(Error error) =>
        Error(error.Description);

    public static void Error(List<Error> errors) =>
        Error(string.Join("\n", errors.Select(e => e.Description)));

    public static void Empty(int count = 1) { }
}
