namespace PrintFlow_V2.Config;

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

    public List<string> GetPrinterPaths()
    {
        List<string> printerPaths = [];

        var labelPrinters = LabelPrintersDict();
        var packingListPrinters = PackingListPrintersDict();

        foreach (KeyValuePair<string, string[]> kvp in labelPrinters)
            printerPaths.AddRange(kvp.Value);

        foreach (KeyValuePair<string, string> kvp in packingListPrinters)
            printerPaths.Add(kvp.Value);

        return printerPaths;
    }

    public Dictionary<string, string[]> LabelPrintersDict()
    {
        Dictionary<string, string[]> printerPaths = [];

        string[] copLabelPrinterPaths = Directory.GetDirectories(
            Path.Combine(BarprnDir.Path, "cops")
        );

        string[] popLabelPrinterPaths = Directory.GetDirectories(
            Path.Combine(BarprnDir.Path, "pops")
        );

        foreach (string copPath in copLabelPrinterPaths)
        {
            string printerName = Path.GetFileName(copPath).Split('-')[^1];
            string popPath = popLabelPrinterPaths.Single(p =>
                Path.GetFileName(p).Split('-')[^1] == printerName
            );

            printerPaths.Add(printerName, [copPath, popPath]);
        }

        return printerPaths;
    }

    public Dictionary<string, string> PackingListPrintersDict()
    {
        Dictionary<string, string> printerPaths = [];

        string[] packingListPrinterPaths = Directory.GetDirectories(
            Path.Combine(BarprnDir.Path, "packing-list")
        );

        foreach (string path in packingListPrinterPaths)
            printerPaths.Add(Path.GetFileName(path), path);

        return printerPaths;
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

    public List<string> GetAllPaths()
    {
        List<string> allPaths = [];

        allPaths.AddRange([.. ToList().Select(desc => desc.Path)]);
        allPaths.AddRange(GetPrinterPaths());

        return allPaths;
    }

    public void Defaults(bool isTest)
    {
        string testBaseDir = @"C:\Temp";

        foreach (var desc in ToList())
        {
            var relative = isTest ? desc.TestRelative : desc.ProdRelative;
            desc.Path = !isTest ? relative : Path.Combine(testBaseDir, relative);
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
