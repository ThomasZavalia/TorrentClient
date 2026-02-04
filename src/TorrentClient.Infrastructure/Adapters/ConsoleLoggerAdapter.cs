using System;
using System.Collections.Generic;
using System.Text;
using TorrentClient.Application.Common.Interfaces;

namespace TorrentClient.Infrastructure.Adapters
{
    public class ConsoleLoggerAdapter : ILogger
    {
        public void LogInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[INFO]  {DateTime.Now:HH:mm:ss} - {message}");
            Console.ResetColor();
        }

        public void LogWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARN]  {DateTime.Now:HH:mm:ss} - {message}");
            Console.ResetColor();
        }

        public void LogError(string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} - {message}");
            if (ex != null)
            {
                Console.WriteLine($"        Exception: {ex.Message}");
                Console.WriteLine($"        {ex.StackTrace}");
            }
            Console.ResetColor();
        }
    }
}
