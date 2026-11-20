using Microsoft.Extensions.Logging;

namespace Snake.Listener
{
    /// <summary>
    /// Конфигурация для файлового логгера
    /// </summary>
    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath,
            bool isJson = false, LogLevel minimumLevel = LogLevel.Information,
            long fileSizeLimitBytes = 10_485_760)
        {
            builder.AddProvider(new FileLoggerProvider(filePath, isJson, minimumLevel, fileSizeLimitBytes));
            return builder;
        }
    }
}