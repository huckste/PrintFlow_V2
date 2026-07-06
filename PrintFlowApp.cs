namespace PrintFlow_V2;

using ErrorOr;
using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;
using PrintFlow_V2.Services;
using PrintFlow_V2.UI;
using PrintFlow_V2.Views;

public class PrintFlowApp
{
    private static PathSchema _pathSchema = new();

    public static async Task Run()
    {
        var ensured = EnsureConfig();

        if (ensured.IsError)
            return;

        _pathSchema = ensured.Value;

        var state = new PrintState(_pathSchema);

        var printers = PrinterService.GetPrinters(_pathSchema).LogOnError();

        if (!printers.IsError)
            state.Printers.AddRange(printers.Value);

        state.Initialize();

        var menu = new Menu(MainMenu.Items(state));
        menu.Show();
    }

    private static ErrorOr<PathSchema> EnsureConfig()
    {
        if (!ConfigManager.ConfigExists())
        {
            Messages.Warning(Err.NotFound(Err.NotFoundType.File, "config.json"));

            if (!Prompts.Confirm("Create default config?", false))
                return Err.FailedTo(Err.Action.Complete, "config setup");

            var created = ConfigManager.Create().LogOnError();

            if (created.IsError)
                return created.Errors;
        }

        while (true)
        {
            var loaded = ConfigManager.Load().LogOnError();

            if (loaded.IsError)
            {
                if (!Prompts.Confirm("Open config menu to fix?", false))
                    return loaded.Errors;

                new ConfigMenu(new PathSchema()).Run();
                continue;
            }

            var schema = loaded.Value;
            var validation = ConfigManager.ValidatePaths(schema);

            if (!validation.IsError)
                return schema;

            Messages.Error(validation.Errors);

            bool onlyMissingDirs = validation.Errors.All(e => e.Code.Contains("NotFound"));

            if (onlyMissingDirs && Prompts.Confirm("Create missing directories?", false))
            {
                ConfigManager.CreateDirectories(schema).LogOnError();
                continue;
            }

            if (!Prompts.Confirm("Open config menu to fix?", false))
                return validation.Errors;

            new ConfigMenu(schema).Run();
        }
    }
}
