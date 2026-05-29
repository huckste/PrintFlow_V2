using PrintFlow_V2.Models;
using PrintFlow_V2.Services;
using PrintFlow_V2.UI;
using PrintFlow_V2.Views;

var state = new PrintState();
state.Printers.AddRange(PrinterService.GetPrinters());
state.Initialize(@"C:\Temp\Label_Data_Load");

var menu = new Menu(MainMenu.Items(state));
menu.Show();
