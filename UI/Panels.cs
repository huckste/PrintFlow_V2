namespace PrintFlow_V2.UI;

using Spectre.Console;

public class Panels
{
    public static void MarkupList(
        IEnumerable<Markup> markup,
        string header,
        string headerColor,
        Color borderColor
    )
    {
        AnsiConsole.Clear();

        var panel = new Panel(new Rows(markup))
            .Header($"[{headerColor}] {header} [/]", Justify.Center)
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(borderColor))
            .Padding(6, 1);

        Messages.Empty(1);
        AnsiConsole.Write(Align.Center(panel));
        Console.ReadKey(true);
    }
}
