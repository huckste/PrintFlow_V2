namespace PrintFlow_V2.Models;

public class MenuItem(string name, Action onSelect, string? key = null, string? icon = null)
{
    public string Name { get; } = name;
    public Action OnSelect { get; } = onSelect;
    public string? Key { get; } = key;
    public string? Icon { get; } = icon;
}
