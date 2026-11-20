using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Snake.Listener
{
    public class FileLogger : ILogger
    {
        private readonly string _filePath;
        private readonly bool _isJson;
        private readonly LogLevel _minimumLevel;
        private readonly long _fileSizeLimit;
        private readonly object _lock = new object();

        public FileLogger(string filePath, bool isJson, LogLevel minimumLevel, long fileSizeLimit)
        {
            _filePath = filePath;
            _isJson = isJson;
            _minimumLevel = minimumLevel;
            _fileSizeLimit = fileSizeLimit;

            // Создаем директорию для логов
            var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null)
                return;

            var logEntry = _isJson
                ? CreateJsonLogEntry(logLevel, message, exception)
                : CreateTextLogEntry(logLevel, message, exception);

            WriteToFile(logEntry);
        }

        private string CreateTextLogEntry(LogLevel logLevel, string message, Exception exception)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logLevel.ToString().ToUpperInvariant();

            var logEntry = $"[{timestamp}] [{level}] {message}";

            if (exception != null)
            {
                logEntry += $"{Environment.NewLine}{exception}";
            }

            return logEntry + Environment.NewLine;
        }

        private string CreateJsonLogEntry(LogLevel logLevel, string message, Exception exception)
        {
            var logObject = new
            {
                Timestamp = DateTime.Now,
                Level = logLevel.ToString(),
                Message = message,
                Exception = exception?.ToString()
            };

            return JsonSerializer.Serialize(logObject) + Environment.NewLine;
        }

        private void WriteToFile(string logEntry)
        {
            lock (_lock)
            {
                try
                {
                    // Ротация логов по размеру
                    var fileInfo = new FileInfo(_filePath);
                    if (fileInfo.Exists && fileInfo.Length > _fileSizeLimit)
                    {
                        RotateLogFile();
                    }

                    File.AppendAllText(_filePath, logEntry);
                }
                catch
                {
                    // Если не можем писать в файл, игнорируем
                }
            }
        }

        private void RotateLogFile()
        {
            var basePath = _filePath.Replace("{Date}", DateTime.Now.ToString("yyyyMMdd"));
            var directory = Path.GetDirectoryName(basePath);
            var fileName = Path.GetFileNameWithoutExtension(basePath);
            var extension = Path.GetExtension(basePath);

            for (int i = 9; i >= 1; i--)
            {
                var oldFile = Path.Combine(directory, $"{fileName}.{i}{extension}");
                var newFile = Path.Combine(directory, $"{fileName}.{i + 1}{extension}");

                if (File.Exists(oldFile))
                {
                    File.Move(oldFile, newFile, true);
                }
            }

            if (File.Exists(basePath))
            {
                File.Move(basePath, Path.Combine(directory, $"{fileName}.1{extension}"), true);
            }
        }
    }
}