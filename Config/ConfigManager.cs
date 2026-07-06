namespace PrintFlow_V2.Config;

using System.Text.Json;
using System.Text.Json.Serialization;
using ErrorOr;
using PrintFlow_V2.Errors;

public static class ConfigManager
{
    private static readonly string _configFilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "config.json"
    );

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static bool ConfigExists() => File.Exists(_configFilePath);

    public static ErrorOr<Success> Create() =>
        Save(PathSchema.Production()).Then(r => Result.Success);

    public static ErrorOr<PathSchema> Load() =>
        Safely.Run(
            () =>
            {
                string json = File.ReadAllText(_configFilePath);
                var schema = JsonSerializer.Deserialize<PathSchema>(json, JsonOptions);
                return schema ?? throw new InvalidOperationException("Deserialized to null");
            },
            Err.Action.Read,
            _configFilePath
        );

    public static ErrorOr<Success> Save(PathSchema settings)
    {
        return Safely.Run(
            () =>
            {
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_configFilePath, json);
            },
            Err.Action.Write,
            _configFilePath,
            "Unable to write to file"
        );
    }

    public static ErrorOr<Success> ValidatePaths(PathSchema pathSchema)
    {
        List<Error> errors = [];

        foreach (var kvp in pathSchema.ToDict())
        {
            string path = kvp.Value.Path;
            string pathFor = kvp.Key;

            if (string.IsNullOrWhiteSpace(path))
            {
                errors.Add(Err.FailedTo(Err.Action.Validate, pathFor));
                continue;
            }

            if (!Directory.Exists(path))
                errors.Add(Err.NotFound(Err.NotFoundType.Directory, path));
        }

        return errors.Count > 0 ? errors : Result.Success;
    }

    public static ErrorOr<Success> CreateDirectories(PathSchema pathSchema)
    {
        List<Error> errors = [];

        var paths = pathSchema.GetAllPaths().CollectTo(errors);

        if (!paths.IsError)
        {
            foreach (string path in paths.Value)
                Safely
                    .Run(() => Directory.CreateDirectory(path), Err.Action.Create, path)
                    .CollectTo(errors);
        }

        return errors.Count > 0 ? errors : Result.Success;
    }
}
