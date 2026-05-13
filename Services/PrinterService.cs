namespace PrintFlow_V2.Services;

public class PrinterService
{
    private static string _printersDir = @"\\ind-as11a\barprn\cops";

    public static (List<string>, int) GetNames()
    {
        List<string> printerNames = Directory
            .GetDirectories(_printersDir)
            .Select(Path.GetFileName)
            .ToList();

        int maxLen = printerNames.Max(name => name.Length);

        return (printerNames, maxLen);
    }
}
