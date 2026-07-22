namespace PrintFlow_V2;

using ErrorOr;
using PrintFlow_V2.Config;
using PrintFlow_V2.Errors;
using PrintFlow_V2.UI;

public class PrintFlowApp
{
    public static PathSchema? EnsureConfig()
    {
        if (!ConfigManager.ConfigExists())
        {
            Messages.Warning(
                Err.NotFound(Err.NotFoundType.File, "config.json").Description,
                "Config Not Found"
            );

            if (!Dialogs.Confirm("Create default config?"))
                return null;

            var created = ConfigManager.Create().LogOnError();

            if (created.IsError)
                return null;
        }

        while (true)
        {
            var loaded = ConfigManager.Load().LogOnError();

            if (loaded.IsError)
            {
                if (!Dialogs.Confirm("Config failed to load. Open config menu to fix?"))
                    return null;

                new ConfigMenu(new PathSchema()).Run();
                continue;
            }

            var schema = loaded.Value;
            var validation = ConfigManager.ValidatePaths(schema);

            if (!validation.IsError)
                return schema;

            Messages.Error(validation.Errors);

            bool onlyMissingDirs = validation.Errors.All(e => e.Code.Contains("NotFound"));

            if (onlyMissingDirs && Dialogs.Confirm("Create missing directories?"))
            {
                ConfigManager.CreateDirectories(schema).LogOnError();
                continue;
            }

            if (!Dialogs.Confirm("Open config menu to fix?"))
                return null;

            new ConfigMenu(schema).Run();
        }
    }
}
