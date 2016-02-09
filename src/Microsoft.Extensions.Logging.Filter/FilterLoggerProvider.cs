using System;

namespace Microsoft.Extensions.Logging.Filter.Internal
{
    public class FilterLoggerProvider : ILoggerProvider
    {
        private ILoggerProvider _loggerProvider;
        private IFilterLoggerSettings _settings;

        public FilterLoggerProvider(
            ILoggerProvider loggerProvider, 
            IFilterLoggerSettings settings)
        {
            _loggerProvider = loggerProvider;
            _settings = settings;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = _loggerProvider.CreateLogger(categoryName);
            var wrappedLogger = new FilterLogger(logger, categoryName, _settings);
            return wrappedLogger;
        }

        public void Dispose()
        {
            _loggerProvider.Dispose();
        }
    }
}