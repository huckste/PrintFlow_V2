namespace PrintFlow_V2.UI;

using ErrorOr;
using Spectre.Console;
using Spectre.Console.Rendering;

public class Messages
{
    public static void MessagePanel(
        MessageType type,
        string header,
        string? message = null,
        List<IRenderable>? rows = null
    )
    {
        Panel panel;

        (string symbol, string color, Color borderColor) = type switch
        {
            MessageType.Error => ("x", "red", Color.Red),
            MessageType.Success => (":check_mark:", "green", Color.Green),
            MessageType.Warning => (":warning:", "yellow", Color.Yellow),
            _ => ("", "", Color.Black),
        };

        if (rows != null && message == null)
        {
            panel = new Panel(new Rows(rows))
                .Header($"[{color}] {rows.Count} {header} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(borderColor)
                .Padding(2, 1);
        }
        else
        {
            var markup = new Markup($"[{color}]{symbol}[/]  {message}");

            panel = new Panel(markup)
                .Header($"[{color}] {header} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(borderColor)
                .Padding(2, 1);
        }

        AnsiConsole.Write(panel);
        Console.ReadKey(true);
    }

    public static void Success(string success, string? title = null) =>
        MessagePanel(MessageType.Success, title ?? "Success", success);

    public static void Error(List<Error> errors)
    {
        var rows = errors
            .Select(e => new Markup($"[red]x[/] {Markup.Escape(e.Description)}"))
            .Cast<IRenderable>()
            .ToList();

        MessagePanel(MessageType.Error, "Error", null, rows);
    }

    public static void Error(Error error) =>
        MessagePanel(MessageType.Error, "Error", error.Description);

    public static void Warning(Error warning) =>
        MessagePanel(MessageType.Warning, "Warning", warning.Description);

    public static void Warning(string warning, string? title = null) =>
        MessagePanel(MessageType.Warning, title ?? "Warning", warning);

    public static void Empty(int count)
    {
        for (int i = 0; i < count; i++)
        {
            AnsiConsole.WriteLine();
        }
    }

    public enum MessageType
    {
        Error,
        Success,
        Warning,
    }
}
