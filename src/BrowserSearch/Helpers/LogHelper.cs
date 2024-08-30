namespace BrowserSearch.Helpers;

using System;

public static class LogHelper
{

    public static void Log(string message, ConsoleColor? color = null, bool? linefeed = true)
    {
        if (color is not null)
        {
            Console.ForegroundColor = color.Value;
        }

        Console.Write(message);

        if (linefeed == true)
        {
            Console.WriteLine();
        }

        if (color is not null)
        {
            Console.ResetColor();
        }
    }

    public static void LogInformation(string message, bool? linefeed = true)
    {
        Log(message, linefeed: linefeed);
    }

    public static void LogError(string message, bool? linefeed = true)
    {
        Log(message, ConsoleColor.Red, linefeed);
    }

    public static void LogWarning(string message, bool? linefeed = true)
    {
        Log(message, ConsoleColor.Yellow, linefeed);
    }
}
