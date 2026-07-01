namespace PrintFlow_V2.Services;

using PrintFlow_V2.Config;
using PrintFlow_V2.Models;

public class PrinterService(PathSchema pathSchema)
{
    public PathSchema pathSchema = pathSchema;

    public List<Printer> GetPrinters()
    {
        Dictionary<string, string[]> printers = pathSchema.LabelPrintersDict();

        int maxLen = printers.Max(name => name.Key.Length);

        // kvp.Value[0] is the cop path and kvp.Value[1] is the pop path
        return
        [
            .. printers.Select(
                (kvp, i) => new Printer(kvp.Key, kvp.Value[0], kvp.Value[1], maxLen)
            ),
        ];
    }
}
