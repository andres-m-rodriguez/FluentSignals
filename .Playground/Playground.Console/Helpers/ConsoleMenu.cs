namespace Playground.Console.Helpers;

using Console = System.Console;

public class ConsoleMenu
{
    private readonly string _title;
    private readonly List<MenuItem> _items = new();
    private int _selectedIndex = 0;

    public ConsoleMenu(string title)
    {
        _title = title;
    }

    public ConsoleMenu AddItem(string text, Action action)
    {
        _items.Add(new MenuItem { Text = text, Action = action });
        return this;
    }

    public ConsoleMenu AddSeparator()
    {
        _items.Add(new MenuItem { IsSeparator = true });
        return this;
    }

    public void Show()
    {
        Console.CursorVisible = false;
        
        while (true)
        {
            Console.Clear();
            ConsoleHelper.WriteHeader(_title);
            
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                
                if (item.IsSeparator)
                {
                    ConsoleHelper.WriteSeparator();
                    continue;
                }

                if (i == _selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.WriteLine($" > {item.Text} ");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"   {item.Text} ");
                }
            }

            Console.WriteLine();
            ConsoleHelper.WriteInfo("Use ↑↓ arrows to navigate, Enter to select, Esc to exit");

            var key = Console.ReadKey(true);
            
            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    _selectedIndex = GetPreviousValidIndex();
                    break;
                    
                case ConsoleKey.DownArrow:
                    _selectedIndex = GetNextValidIndex();
                    break;
                    
                case ConsoleKey.Enter:
                    if (_selectedIndex >= 0 && _selectedIndex < _items.Count && !_items[_selectedIndex].IsSeparator)
                    {
                        Console.Clear();
                        Console.CursorVisible = true;
                        _items[_selectedIndex].Action?.Invoke();
                        Console.WriteLine();
                        Console.WriteLine("Press any key to return to menu...");
                        Console.ReadKey(true);
                        Console.CursorVisible = false;
                    }
                    break;
                    
                case ConsoleKey.Escape:
                    Console.CursorVisible = true;
                    Console.Clear();
                    return;
            }
        }
    }

    private int GetNextValidIndex()
    {
        var next = _selectedIndex;
        do
        {
            next = (next + 1) % _items.Count;
        } while (_items[next].IsSeparator && next != _selectedIndex);
        
        return next;
    }

    private int GetPreviousValidIndex()
    {
        var prev = _selectedIndex;
        do
        {
            prev = prev - 1 < 0 ? _items.Count - 1 : prev - 1;
        } while (_items[prev].IsSeparator && prev != _selectedIndex);
        
        return prev;
    }

    private class MenuItem
    {
        public string Text { get; set; } = "";
        public Action? Action { get; set; }
        public bool IsSeparator { get; set; }
    }
}