using PrintFlow_V2.Config;
using Spectre.Console;

namespace PrintFlow_V2.UI;

public class ConfigMenu(PathSchema pathSchema)
{
    private PathSchema _pathSchema = pathSchema;

    public void Run()
    {
        bool done = false;

        while (!done)
        {
            AnsiConsole.Clear();
            done = DisplayMenu();
        }
    }

    private bool DisplayMenu()
    {
        List<string> choices = ["Edit Paths", "Validate", "Load Test", "Load Production", "Save"];
        var choice = Prompts.SingleSelect("Config Menu", choices);

        if (choice == null)
            return true;

        return choice switch
        {
            var s when s.Contains("Edit") => EditPaths(),
            var s when s.Contains("Validate") => ValidatePaths(),
            var s when s.Contains("Test") => Load(false),
            var s when s.Contains("Production") => Load(true),
            var s when s.Contains("Save") => Save(),
            _ => true,
        };
    }

    private bool EditPaths()
    {
        while (true)
        {
            AnsiConsole.Clear();

            var choices = new List<string>();
            var pathsDict = _pathSchema.ToDict();

            foreach (var pathDesc in pathsDict)
                choices.Add($"{pathDesc.Value.Name}: {Truncate(pathDesc.Value.Path)}");

            var choice = Prompts.SingleSelect("Edit Paths", choices);

            if (choice != null)
            {
                if (pathsDict.TryGetValue(choice.Split(':')[0].Trim(), out var desc))
                    EditPath(desc.Name, desc.Path, v => desc.Path = v);
            }

            return false;
        }
    }

    private static void EditPath(
        string name,
        string currentValue,
        Action<string> setter,
        bool isFile = false
    )
    {
        Messages.Empty(1);
        AnsiConsole.MarkupLine($"[dim]{name}[/]");
        AnsiConsole.MarkupLine($"[dim]Current:[/]{currentValue}");
        Messages.Empty(1);

        var newValue = Prompts.TextInput("New path", "to keep", currentValue);

        if (newValue != null && newValue != currentValue)
        {
            if (newValue.StartsWith('~'))
            {
                newValue = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    newValue[1..].TrimStart('/', '\\')
                );
            }

            setter(newValue);

            if (isFile)
            {
                if (File.Exists(newValue))
                {
                    Messages.Success("File exists");
                }
                else
                {
                    Messages.Warning("File not Found");
                }
            }
            else
            {
                if (Directory.Exists(newValue))
                {
                    Messages.Success("Directory exists");
                }
                else
                {
                    Messages.Warning("Will be Created");
                }
            }
        }
    }

    public bool ValidatePaths()
    {
        Messages.Empty(1);

        var result = ConfigManager.ValidatePaths(_pathSchema);

        result.Switch(
            success =>
            {
                Panels.StringList(
                    _pathSchema.GetAllPaths(),
                    "All paths valid",
                    "green",
                    Color.Green
                );
            },
            errors =>
            {
                Messages.Error(errors);
                Messages.Empty(1);

                bool isOnlyMissingDirs = errors.All(e => e.Code.Contains("NotFound"));

                if (isOnlyMissingDirs)
                {
                    if (Prompts.Confirm("Create missing directories", false))
                    {
                        result = ConfigManager.CreateDirectories(_pathSchema);

                        if (result.IsError)
                            Messages.Error(errors);
                    }
                }
            }
        );

        return false;
    }

    private bool Load(bool isProd)
    {
        if (isProd)
        {
            if (Prompts.Confirm("Load production paths?", false))
            {
                _pathSchema = PathSchema.Production();
                Messages.Empty(1);
                Messages.Success("Production paths loaded");
            }
        }
        else
        {
            if (Prompts.Confirm("Load test paths?", false))
            {
                _pathSchema = PathSchema.Test();
                Messages.Empty(1);
                Messages.Success("Test paths loaded");
            }
        }

        return false;
    }

    private static string Truncate(string path, int maxLength = 35)
    {
        if (string.IsNullOrEmpty(path))
            return "[dim](not set)[/]";

        if (path.Length <= maxLength)
            return $"[dim]{path}[/]";

        return $"[dim]...{path[^(maxLength - 3)..]}[/]";
    }

    private bool Save()
    {
        var result = ConfigManager.Save(_pathSchema);

        result.Switch(
            success => Messages.Success("Configuration Saved"),
            errors => Messages.Error(result.Errors)
        );

        return true;
    }
}
