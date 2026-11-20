using Microsoft.Extensions.Logging;

namespace Snake.Listener
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _filePath;
        private readonly bool _isJson;
        private readonly LogLevel _minimumLevel;
        private readonly long _fileSizeLimit;

        public FileLoggerProvider(string filePath, bool isJson, LogLevel minimumLevel, long fileSizeLimit)
        {
            _filePath = filePath;
            _isJson = isJson;
            _minimumLevel = minimumLevel;
            _fileSizeLimit = fileSizeLimit;
        }

        public ILogger CreateLogger(string categoryName) =>
            new FileLogger(_filePath, _isJson, _minimumLevel, _fileSizeLimit);

        public void Dispose()
        {
        }
    }
}