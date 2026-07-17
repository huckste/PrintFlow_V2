using PrintFlow_V2;
using Serilog;

string tempPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    "Temp"
);

Console.OutputEncoding = System.Text.Encoding.UTF8;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File(Path.Combine(tempPath, "logs"), rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    await PrintFlowApp.Run();
}
finally
{
    await Log.CloseAndFlushAsync();
}
