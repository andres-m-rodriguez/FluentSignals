namespace Playground.Console.Helpers;

using Console = System.Console;

public static class ConsoleHelper
{
    public static void WriteHeader(string title, ConsoleColor color = ConsoleColor.Cyan)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        
        var line = new string('=', title.Length + 4);
        Console.WriteLine();
        Console.WriteLine(line);
        Console.WriteLine($"  {title}  ");
        Console.WriteLine(line);
        Console.WriteLine();
        
        Console.ForegroundColor = originalColor;
    }

    public static void WriteSuccess(string message)
    {
        WriteColored($"✓ {message}", ConsoleColor.Green);
    }

    public static void WriteError(string message)
    {
        WriteColored($"✗ {message}", ConsoleColor.Red);
    }

    public static void WriteInfo(string message)
    {
        WriteColored($"ℹ {message}", ConsoleColor.Cyan);
    }

    public static void WriteWarning(string message)
    {
        WriteColored($"⚠ {message}", ConsoleColor.Yellow);
    }

    public static void WriteColored(string message, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    public static void WriteSeparator(char character = '-', int length = 50)
    {
        Console.WriteLine(new string(character, length));
    }

    public static void WriteProgress(string label, int current, int total)
    {
        var percentage = (int)((current / (double)total) * 100);
        var filled = (int)((current / (double)total) * 20);
        var empty = 20 - filled;
        
        Console.Write($"\r{label}: [");
        Console.Write(new string('█', filled));
        Console.Write(new string('░', empty));
        Console.Write($"] {percentage}%");
        
        if (current >= total)
            Console.WriteLine();
    }

    public static void ClearLine()
    {
        var windowWidth = 80; // Default console width
        Console.Write("\r" + new string(' ', windowWidth - 1) + "\r");
    }

    public static void WriteTable<T>(IEnumerable<T> items, params (string Header, Func<T, object> GetValue)[] columns)
    {
        var itemsList = items.ToList();
        if (!itemsList.Any()) return;

        // Calculate column widths
        var widths = new int[columns.Length];
        for (int i = 0; i < columns.Length; i++)
        {
            widths[i] = Math.Max(
                columns[i].Header.Length,
                itemsList.Max(item => columns[i].GetValue(item)?.ToString()?.Length ?? 0)
            );
        }

        // Print header
        Console.WriteLine();
        for (int i = 0; i < columns.Length; i++)
        {
            Console.Write(columns[i].Header.PadRight(widths[i] + 3));
        }
        Console.WriteLine();
        
        // Print separator
        for (int i = 0; i < columns.Length; i++)
        {
            Console.Write(new string('-', widths[i] + 2) + " ");
        }
        Console.WriteLine();

        // Print rows
        foreach (var item in itemsList)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                var value = columns[i].GetValue(item)?.ToString() ?? "";
                Console.Write(value.PadRight(widths[i] + 3));
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
}