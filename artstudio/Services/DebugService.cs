using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace artstudio.Services
{
    public interface IDebugService
    {
        void LogInfo(string message);
        void LogError(string message, Exception? exception = null);
        void LogWarning(string message);
        void LogDebug(string message);
        Task WriteToFileAsync(string message);
    }

    public class DebugService : IDebugService
    {
        private readonly ILogger<DebugService>? _logger;
        private readonly string _logFilePath;

        public DebugService(ILogger<DebugService>? logger = null)
        {
            _logger = logger;
            _logFilePath = Path.Combine(FileSystem.AppDataDirectory, "debug.log");
        }

        public void LogInfo(string message)
        {
            var logMessage = $"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";

            // Use multiple output methods to ensure we can see the logs
            Debug.WriteLine(logMessage);
            System.Console.WriteLine(logMessage);
            _logger?.LogInformation(message);

            // Also write to file
            _ = Task.Run(() => WriteToFileAsync(logMessage));
        }

        public void LogError(string message, Exception? exception = null)
        {
            var logMessage = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";
            if (exception != null)
            {
                logMessage += $"\nException: {exception.Message}\nStackTrace: {exception.StackTrace}";
            }

            Debug.WriteLine(logMessage);
            System.Console.WriteLine(logMessage);

            if (exception != null)
                _logger?.LogError(exception, message);
            else
                _logger?.LogError(message);

            _ = Task.Run(() => WriteToFileAsync(logMessage));
        }

        public void LogWarning(string message)
        {
            var logMessage = $"[WARNING] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";

            Debug.WriteLine(logMessage);
            System.Console.WriteLine(logMessage);
            _logger?.LogWarning(message);

            _ = Task.Run(() => WriteToFileAsync(logMessage));
        }

        public void LogDebug(string message)
        {
            var logMessage = $"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {message}";

            Debug.WriteLine(logMessage);
            System.Console.WriteLine(logMessage);
            _logger?.LogDebug(message);

            _ = Task.Run(() => WriteToFileAsync(logMessage));
        }

        public async Task WriteToFileAsync(string message)
        {
            try
            {
                using var writer = new StreamWriter(_logFilePath, append: true);
                await writer.WriteLineAsync(message);
                await writer.FlushAsync();
            }
            catch
            {
                // Ignore file writing errors to prevent infinite loops
            }
        }

        public async Task<string> GetLogContentsAsync()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    return await File.ReadAllTextAsync(_logFilePath);
                }
                return "No log file found.";
            }
            catch (Exception ex)
            {
                return $"Error reading log file: {ex.Message}";
            }
        }

        public async Task ClearLogAsync()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    await Task.Run(() => File.Delete(_logFilePath));
                }
            }
            catch (Exception ex)
            {
                LogError("Error clearing log file", ex);
            }
        }
    }
}