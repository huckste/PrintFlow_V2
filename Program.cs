using PrintFlow_V2.UI;
using PrintFlow_V2.Views;
using Serilog;
using Terminal.Gui.App;

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
    using IApplication app = Application.Create();
    app.Init();

    TuiApp.App = app;

    using var window = new MainWindow();
    app.Run(window);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception caused application to crash");
}
finally
{
    await Log.CloseAndFlushAsync();
}
