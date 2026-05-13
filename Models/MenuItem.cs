namespace PrintFlow_V2.Models;

public class MenuItem
{
    public string Name { get; }
    public Action OnSelect { get; }
    public string? Key { get; }
    public string? Icon { get; }

    public MenuItem(string name, Action onSelect, string? key = null, string? icon = null)
    {
        Name = name;
        OnSelect = onSelect;
        Key = key;
        Icon = icon;
    }
}
