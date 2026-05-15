namespace PrintFlow_V2.Services;

using PrintFlow_V2.Models;

public class PrinterService
{
    private static readonly string _printersDir = @"\\ind-as11a\barprn\cops";

    public static List<Printer> GetPrinters()
    {
        string[] printerPaths = Directory.GetDirectories(_printersDir);
        string[] printerNames =
        [
            .. printerPaths.Select(printerPaths => Path.GetFileName(printerPaths)),
        ];

        int maxLen = printerNames.Max(name => name.Length);

        return
        [
            .. printerPaths.Select(
                (printerPath, i) => new Printer(printerNames[i].Split('-')[^1], printerPath, maxLen)
            ),
        ];
    }
}
