using PrintFlow_V2.Models;
using Spectre.Console;

namespace PrintFlow_V2.UI;

public class Menu
{
    private readonly string _title;
    private readonly List<MenuItem> _items;
    private int _selectedIndex;
    private int _menuStartRow; // Console row where menu items start, used for partial redraws

    // ANSI Shadow font — "PRINT FLOW" stacked, matching LazyVim's bold block style
    private static readonly string[] Logo =
    [
        @"██████╗ ██████╗ ██╗███╗   ██╗████████╗ ███████╗██╗      ██████╗ ██╗    ██╗",
        @"██╔══██╗██╔══██╗██║████╗  ██║╚══██╔══╝ ██╔════╝██║     ██╔═══██╗██║    ██║",
        @"██████╔╝██████╔╝██║██╔██╗ ██║   ██║    █████╗  ██║     ██║   ██║██║ █╗ ██║",
        @"██╔═══╝ ██╔══██╗██║██║╚██╗██║   ██║    ██╔══╝  ██║     ██║   ██║██║███╗██║",
        @"██║     ██║  ██║██║██║ ╚████║   ██║    ██║     ███████╗╚██████╔╝╚███╔███╔╝",
        @"╚═╝     ╚═╝  ╚═╝╚═╝╚═╝  ╚═══╝   ╚═╝    ╚═╝     ╚══════╝ ╚═════╝  ╚══╝╚══╝ ",
    ];

    public Menu(string title, List<MenuItem> items)
    {
        _title = title;
        _items = items;
        _selectedIndex = 0;
    }

    /// <summary>
    /// Main loop — draws the full UI once, then listens for keypresses.
    /// Navigation only redraws the menu area (not the logo) to avoid flicker.
    /// </summary>
    public void Show()
    {
        Console.CursorVisible = false;
        DrawFull();

        while (true)
        {
            var key = Console.ReadKey(true);

            // Check if the keypress matches a menu item's keybind shortcut
            var pressed = key.KeyChar.ToString().ToLower();
            var match = _items.FindIndex(i => i.Key?.ToLower() == pressed && i.Name != "Quit");
            if (match >= 0)
            {
                Console.CursorVisible = true;
                AnsiConsole.Clear();
                _items[match].OnSelect();
                Console.CursorVisible = false;
                DrawFull(); // Full redraw after returning from action
                continue;
            }

            switch (key.Key)
            {
                // Vim-style j/k and arrow key navigation with wraparound
                case ConsoleKey.UpArrow or ConsoleKey.K:
                    _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;
                    RedrawMenu();
                    break;
                case ConsoleKey.DownArrow or ConsoleKey.J:
                    _selectedIndex = (_selectedIndex + 1) % _items.Count;
                    RedrawMenu();
                    break;

                // Execute selected item's action
                case ConsoleKey.Enter:
                    if (_items[_selectedIndex].Name == "Quit")
                    {
                        AnsiConsole.Clear();
                        Console.CursorVisible = true;
                        return;
                    }
                    Console.CursorVisible = true;
                    AnsiConsole.Clear();
                    _items[_selectedIndex].OnSelect();
                    Console.CursorVisible = false;
                    DrawFull();
                    break;

                // Quick exit
                case ConsoleKey.Q
                or ConsoleKey.Escape:
                    AnsiConsole.Clear();
                    Console.CursorVisible = true;
                    return;
            }
        }
    }

    /// <summary>
    /// Full screen draw — clears everything and renders logo + menu from scratch.
    /// Called on initial load and after returning from a menu action.
    /// </summary>
    private void DrawFull()
    {
        AnsiConsole.Clear();

        var width = AnsiConsole.Profile.Width;
        var height = AnsiConsole.Profile.Height;

        // Push content into the upper third of the terminal
        var contentHeight = Logo.Length + 4 + (_items.Count * 2);
        var topPad = Math.Max(1, (height - contentHeight) / 3);

        for (var i = 0; i < topPad; i++)
            Console.WriteLine();

        // Render centered logo using raw ANSI escape codes for color
        foreach (var line in Logo)
        {
            if (line == "")
            {
                Console.WriteLine();
                continue;
            }
            var pad = new string(' ', Math.Max(0, (width - line.Length) / 2));
            Console.Write($"\x1b[38;2;110;150;200m{pad}{line}\x1b[0m\n");
        }

        // Breathing room between logo and menu
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();

        // Store the row position so RedrawMenu can jump back here without touching the logo
        _menuStartRow = Console.CursorTop;
        DrawMenuItems();
    }

    /// <summary>
    /// Partial redraw — clears only the menu lines and re-renders them.
    /// The logo stays untouched, preventing flicker during navigation.
    /// </summary>
    private void RedrawMenu()
    {
        Console.SetCursorPosition(0, _menuStartRow);

        // Blank out all menu lines (each item = 2 rows: content + spacing)
        var width = AnsiConsole.Profile.Width;
        var clearLines = _items.Count * 2;
        for (var i = 0; i < clearLines; i++)
        {
            Console.Write(new string(' ', width));
            Console.WriteLine();
        }

        // Reposition and redraw
        Console.SetCursorPosition(0, _menuStartRow);
        DrawMenuItems();
    }

    /// <summary>
    /// Renders all menu items with spacing between each.
    /// </summary>
    private void DrawMenuItems()
    {
        for (var i = 0; i < _items.Count; i++)
        {
            WriteMenuItem(i);
            Console.WriteLine(); // Blank line between items — LazyVim spacing style
        }
    }

    /// <summary>
    /// Renders a single menu item line: [icon]  [name]  ...gap...  [keybind]
    /// Raw ANSI escapes used instead of AnsiConsole.Markup to avoid
    /// cursor positioning issues during partial redraws.
    /// </summary>
    private void WriteMenuItem(int index)
    {
        var width = AnsiConsole.Profile.Width;
        var item = _items[index];
        var icon = item.Icon ?? " ";
        var name = item.Name;
        var keybind = item.Key ?? "";

        // Color palette — selected items are brighter versions of the same hues
        var iconColor =
            index == _selectedIndex ? "\x1b[38;2;130;180;220m" : "\x1b[38;2;90;130;160m";
        var nameColor =
            index == _selectedIndex ? "\x1b[1m\x1b[38;2;100;180;255m" : "\x1b[38;2;100;160;210m";
        var keyColor = index == _selectedIndex ? "\x1b[38;2;230;160;100m" : "\x1b[38;2;180;120;80m";
        var reset = "\x1b[0m";

        // Menu width matches logo width so items align with the header
        var menuWidth = 50;
        var menuLeft = Math.Max(0, (width - menuWidth) / 2);
        var leftPad = new string(' ', menuLeft);

        // Build the line: icon + gap + name + flexible gap + keybind
        var nameSection = $"{icon} {name}";
        var gap = new string(' ', Math.Max(1, menuWidth - nameSection.Length - keybind.Length));

        Console.Write(
            $"{leftPad}{iconColor}{icon}{reset}  {nameColor}{name}{reset}{gap}{keyColor}{keybind}{reset}"
        );
        Console.WriteLine();
    }
}
