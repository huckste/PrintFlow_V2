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

        var layout = new Layout("root").SplitRows(
            new Layout("top"),
            new Layout("mid"),
            new Layout("bot")
        );

        layout["top"].Ratio(1);
        layout["mid"].Size(markup.Count() + 4);
        layout["bot"].Ratio(1);

        layout["top"].Update(new Text(""));
        layout["bot"].Update(new Text(""));
        layout["mid"].Update(Align.Center(panel));

        AnsiConsole.Write(layout);
        Console.ReadKey(true);
    }
}
