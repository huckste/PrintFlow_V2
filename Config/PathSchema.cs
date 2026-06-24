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

    public PathDesc PrinterDir { get; set; } =
        new()
        {
            Name = "Printers Directory",
            Desc = "Directory that contains all printers",
            ProdRelative = @"\\ind-as11a\barprn",
            TestRelative = "printers",
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
        string[] labelPrinters =
        [
            "IND-DC-OCE4000",
            "IND-DC-OCE7400",
            "IND-DC-SATOLP100R",
            "IND-DC-SATOLP100R2",
            "IND-DC-SOLIDF90",
        ];
        string[] packingListPrinters = ["IND-DC-ADM", "IND-DC-MCR"];
        string[] labelTypes = ["pops", "cops"];

        List<string> printerPaths = [];

        foreach (string printer in labelPrinters)
        {
            foreach (string type in labelTypes)
                printerPaths.Add(Path.Combine(PrinterDir.Path, Path.Combine(type, printer)));
        }

        foreach (string printer in packingListPrinters)
            printerPaths.Add(Path.Combine(PrinterDir.Path, Path.Combine("packing-list", printer)));

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
