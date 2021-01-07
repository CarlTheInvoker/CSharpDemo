namespace CSharpDemo
{
    using System;

    public static class Logger
    {
        public static void LogInfo(string message)
        {
            Log(message, " Info  ");
        }

        public static void LogWarning(string message)
        {
            Log(message, "Warning");
        }

        public static void LogError(string message)
        {
            Log(message, " Error ");
        }

        private static void Log(string message, string logLevel)
        {
            Console.WriteLine($"[{DateTime.UtcNow:O}][{logLevel}] - {message}");
        }
    }
}
