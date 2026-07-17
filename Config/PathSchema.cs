namespace PrintFlow_V2.Config;

using ErrorOr;
using PrintFlow_V2.Errors;
using PrintFlow_V2.Models;

public class PathSchema
{
    public PathDesc LabelsDir { get; set; } =
        new()
        {
            Name = "Labels Directory",
            Desc = "Directory where newly added labels are found",
            ProdRelative = @"\\ind-as84\asroot$\labels",
            TestRelative = "labels",
        };

    public PathDesc LabelDataLoad { get; set; } =
        new()
        {
            Name = "Label Data Load",
            Desc = "Directory where availble lables are placed",
            ProdRelative = @"C:\Label_Data_Load",
            TestRelative = "label_data_load",
        };

    public PathDesc BarprnDir { get; set; } =
        new()
        {
            Name = "Barprn Directory",
            Desc = "Directory where printer directories for packinglist, cops, and pops are found",
            ProdRelative = @"\\ind-as11a\barprn",
            TestRelative = "barprn",
        };

    public PathDesc Archive { get; set; } =
        new()
        {
            Name = "Label Archive",
            Desc = "Label Archive Directory",
            ProdRelative = @"C:\Archive",
            TestRelative = "Archive",
        };

    public PathDesc Logs { get; set; } =
        new()
        {
            Name = "Logs",
            Desc = "Dirctory for logs",
            ProdRelative = @"C:\logs",
            TestRelative = "logs",
        };

    public ErrorOr<List<string>> GetPrinterPaths()
    {
        List<string> printerPaths = [];
        List<Error> errors = [];

        var labelPrinters = LabelPrintersDict().CollectTo(errors);
        var packingListPrinters = PackingListPrintersDict().CollectTo(errors);

        if (!packingListPrinters.IsError && !labelPrinters.IsError)
        {
            foreach (KeyValuePair<string, string[]> kvp in labelPrinters.Value)
                printerPaths.AddRange(kvp.Value);

            foreach (KeyValuePair<string, string> kvp in packingListPrinters.Value)
                printerPaths.Add(kvp.Value);

            return printerPaths;
        }

        return errors;
    }

    public ErrorOr<Dictionary<string, string[]>> LabelPrintersDict()
    {
        Dictionary<string, string[]> printerPaths = [];
        List<Error> errors = [];

        string copsPath = Path.Combine(BarprnDir.Path, "cops");
        string popsPath = Path.Combine(BarprnDir.Path, "pops");

        var copResult = Safely
            .Run(() => Directory.GetDirectories(copsPath), Err.Action.Read, copsPath)
            .CollectTo(errors);

        var popResult = Safely
            .Run(() => Directory.GetDirectories(popsPath), Err.Action.Read, popsPath)
            .CollectTo(errors);

        errors.LogToFile();

        if (errors.Count <= 0)
        {
            foreach (string copPath in copResult.Value)
            {
                string printerName = Path.GetFileName(copPath).Split('-')[^1];

                string popPath = popResult.Value.Single(p =>
                    Path.GetFileName(p).Split('-')[^1] == printerName
                );

                printerPaths.Add(printerName, [copPath, popPath]);
            }

            return printerPaths;
        }

        return errors;
    }

    public ErrorOr<Dictionary<string, string>> PackingListPrintersDict()
    {
        Dictionary<string, string> printerPaths = [];
        string pklPath = Path.Combine(BarprnDir.Path, "packing-lists");

        var result = Safely
            .Run(() => Directory.GetDirectories(pklPath), Err.Action.Read, pklPath)
            .LogOnError();

        if (!result.IsError)
        {
            foreach (string path in result.Value)
                printerPaths.Add(Path.GetFileName(path), path);

            return printerPaths;
        }

        return result.Errors;
    }

    public List<PathDesc> ToList()
    {
        return
        [
            .. typeof(PathSchema)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(PathDesc))
                .Select(p => p.GetValue(this) as PathDesc)
                .Where(v => v != null)
                .Cast<PathDesc>(),
        ];
    }

    public Dictionary<string, PathDesc> ToDict() =>
        ToList().ToDictionary(desc => desc.Name, desc => desc);

    public List<string> GetStructuralPaths() =>
        [
            .. ToList().Select(d => d.Path),
            Path.Combine(BarprnDir.Path, "cops"),
            Path.Combine(BarprnDir.Path, "pops"),
            Path.Combine(BarprnDir.Path, "packing-lists"),
        ];

    public ErrorOr<List<string>> GetAllPaths()
    {
        List<string> allPaths = [];
        List<Error> errors = [];

        var printerPaths = GetPrinterPaths().CollectTo(errors);

        allPaths.AddRange([.. ToList().Select(desc => desc.Path)]);

        if (!printerPaths.IsError)
            allPaths.AddRange(printerPaths.Value);

        errors.LogToFile();

        return errors.Count > 0 ? errors : allPaths;
    }

    public string GetTempPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Temp");

    public void Defaults(bool isTest)
    {
        foreach (var desc in ToList())
        {
            var relative = isTest ? desc.TestRelative : desc.ProdRelative;
            desc.Path = !isTest ? relative : Path.Combine(GetTempPath(), relative);
        }
    }

    public static PathSchema Production()
    {
        var schema = new PathSchema();
        schema.Defaults(false);
        return schema;
    }

    public static PathSchema Test()
    {
        var schema = new PathSchema();
        schema.Defaults(true);
        return schema;
    }
}
