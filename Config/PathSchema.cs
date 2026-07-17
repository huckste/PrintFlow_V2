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

    public ErrorOr<List<string>> GetPrinterPaths() =>
        PrintersDict().Then(v => v.SelectMany(outer => outer.Value.Values).ToList());

    public ErrorOr<Dictionary<string, Dictionary<Printer.PrinterType, string>>> PrintersDict()
    {
        List<Error> errors = [];
        Dictionary<string, Dictionary<Printer.PrinterType, string>> printerPaths = [];

        Dictionary<Printer.PrinterType, string> printerParentDirs = new()
        {
            [Printer.PrinterType.COP] = Path.Combine(BarprnDir.Path, "cops"),
            [Printer.PrinterType.POP] = Path.Combine(BarprnDir.Path, "pops"),
            [Printer.PrinterType.PKL] = Path.Combine(BarprnDir.Path, "packing-lists"),
        };

        foreach (var (type, path) in printerParentDirs)
        {
            var result = Safely
                .Run(() => Directory.GetDirectories(path), Err.Action.Read, path)
                .CollectTo(errors);

            if (result.IsError)
                continue;

            foreach (var printerPath in result.Value)
            {
                string printerName = Path.GetFileName(printerPath).Split('-')[^1];

                if (!printerPaths.TryAdd(printerName, new() { [type] = printerPath }))
                    printerPaths[printerName].TryAdd(type, printerPath);
            }
        }

        return errors.Count > 0 ? errors : printerPaths;
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

    public static string GetTempPath() =>
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
