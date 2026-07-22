using System.Collections.ObjectModel;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace PrintFlow_V2.UI;

public static class Dialogs
{
    public static string? SingleSelect(string title, IEnumerable<string> choices)
    {
        var list = choices.ToList();
        string? result = null;

        var dialog = new Dialog<object>
        {
            Title = title,
            Width = 65,
            Height = Math.Min(list.Count + 6, 28),
        };

        var listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        listView.SetSource(new ObservableCollection<string>(list));

        listView.Activated += (s, e) =>
        {
            result = listView.SelectedItem.HasValue ? list.ElementAtOrDefault(listView.SelectedItem.Value) : null;
            TuiApp.App.RequestStop();
        };

        dialog.Add(listView);

        var okBtn = new Button { Text = "OK", IsDefault = true };
        okBtn.Accepting += (s, e) =>
        {
            result = listView.SelectedItem.HasValue ? list.ElementAtOrDefault(listView.SelectedItem.Value) : null;
            TuiApp.App.RequestStop();
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => TuiApp.App.RequestStop();

        dialog.AddButton(cancelBtn);
        dialog.AddButton(okBtn);

        TuiApp.App.Run(dialog);
        dialog.Dispose();

        return result;
    }

    public static List<string>? MultiSelect(string title, IEnumerable<string> choices)
    {
        var list = choices.ToList();
        List<string>? result = null;

        var dialog = new Dialog<object>
        {
            Title = title,
            Width = 70,
            Height = Math.Min(list.Count + 8, 30),
        };

        var hint = new Label
        {
            Text = "Space to toggle, Enter/OK to confirm",
            X = 0,
            Y = 0,
        };

        var listView = new ListView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShowMarks = true,
            MarkMultiple = true,
        };
        listView.SetSource(new ObservableCollection<string>(list));

        dialog.Add(hint, listView);

        var okBtn = new Button { Text = "OK", IsDefault = true };
        okBtn.Accepting += (s, e) =>
        {
            var indices = listView.GetAllMarkedItems().ToList();
            result = indices.Count > 0 ? indices.Select(i => list[i]).ToList() : null;
            TuiApp.App.RequestStop();
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => TuiApp.App.RequestStop();

        dialog.AddButton(cancelBtn);
        dialog.AddButton(okBtn);

        TuiApp.App.Run(dialog);
        dialog.Dispose();

        return result;
    }

    public static bool Confirm(string message, string title = "Confirm")
    {
        return MessageBox.Query(TuiApp.App, title, message, "Yes", "No") == 0;
    }

    public static string? TextInput(string title, string? defaultValue = null)
    {
        string? result = null;

        var dialog = new Dialog<object>
        {
            Title = title,
            Width = 60,
            Height = 8,
        };

        var field = new TextField
        {
            Text = defaultValue ?? "",
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
        };

        dialog.Add(field);

        var okBtn = new Button { Text = "OK", IsDefault = true };
        okBtn.Accepting += (s, e) =>
        {
            var text = field.Text.ToString();
            result = string.IsNullOrWhiteSpace(text) ? defaultValue : text;
            TuiApp.App.RequestStop();
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => TuiApp.App.RequestStop();

        dialog.AddButton(cancelBtn);
        dialog.AddButton(okBtn);

        TuiApp.App.Run(dialog);
        dialog.Dispose();

        return result;
    }

    public static int? IntInput(string title, int defaultValue, int min, int max)
    {
        int? result = null;

        var dialog = new Dialog<object>
        {
            Title = title,
            Width = 50,
            Height = 9,
        };

        var rangeLabel = new Label
        {
            Text = $"Enter a value between {min} and {max}",
            X = 0,
            Y = 0,
        };

        var field = new TextField
        {
            Text = defaultValue.ToString(),
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
        };

        dialog.Add(rangeLabel, field);

        var okBtn = new Button { Text = "OK", IsDefault = true };
        okBtn.Accepting += (s, e) =>
        {
            if (int.TryParse(field.Text.ToString(), out int val) && val >= min && val <= max)
            {
                result = val;
                TuiApp.App.RequestStop();
            }
            else
            {
                MessageBox.ErrorQuery(TuiApp.App, "Invalid Input", $"Enter a number between {min} and {max}", "OK");
            }
        };

        var cancelBtn = new Button { Text = "Cancel" };
        cancelBtn.Accepting += (s, e) => TuiApp.App.RequestStop();

        dialog.AddButton(cancelBtn);
        dialog.AddButton(okBtn);

        TuiApp.App.Run(dialog);
        dialog.Dispose();

        return result;
    }
}
