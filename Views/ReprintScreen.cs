using PrintFlow_V2.Models;
using PrintFlow_V2.UI;
using Spectre.Console;

namespace PrintFlow_V2.Views;

public class ReprintScreen(PrintState state)
{
    private readonly PrintState _state = state;

    public void Show()
    {
        while (true)
        {
            AnsiConsole.Clear();

            var layout = new Layout("Root").SplitColumns(new Layout("Left"), new Layout("Right"));
            AnsiConsole.Write(layout);

            var choice = Prompts.SingleSelect("", ["Reprint By Header", "Reprint By Page Number"]);

            switch (choice)
            {
                case "Reprint By Header":
                    break;
                case "Reprint By Page Number":
                    break;
                case null:
                    return;
            }
        }
    }
}
