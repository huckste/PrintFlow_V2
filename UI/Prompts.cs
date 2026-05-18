namespace PrintFlow_V2.UI;

using Spectre.Console;

public class Prompts
{
    public static List<string>? MultiSelect(
        string title,
        IEnumerable<string> choices,
        string instructions = "Space to toggle, Enter to confirm (empty to cancel)"
    )
    {
        var choice = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"[bold]{title}[/]")
                .InstructionsText($"[dim]{instructions}[/]")
                .Required(false)
                .AddChoices(choices.Prepend("Select All"))
        );

        return choice ?? null;
    }

    public static string? SingleSelect(string title, IEnumerable<string> choices)
    {
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{title}[/]")
                .AddChoices(choices.Append("Cancel"))
        );

        return choice ?? null;
    }

    public static int ValidateInt(string prompt, int defaultValue, int min, int max)
    {
        return AnsiConsole.Prompt(
            new TextPrompt<int>($"[bold]{prompt}[/]")
                .DefaultValue(defaultValue)
                .Validate(n =>
                    n >= min && n <= max
                        ? ValidationResult.Success()
                        : ValidationResult.Error($"Between {min} and {max}")
                )
        );
    }

    public static bool Confirm(string message, bool defaultBool = true)
    {
        return AnsiConsole.Confirm($"[red]{message}[/]", defaultBool);
    }
}
