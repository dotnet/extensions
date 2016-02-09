using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Logging.Filter.Internal
{
    public class FilterLogger : ILogger
    {
        private readonly ILogger _logger;
        private readonly string _categoryName;
        private IFilterLoggerSettings _settings;
        private Func<LogLevel, bool> _isEnabled;

        public FilterLogger(
            ILogger logger,
            string categoryName,
            IFilterLoggerSettings settings)
        {
            _logger = logger;
            _categoryName = categoryName;
            _settings = settings;

            _isEnabled = GetFilter();
        }

        public bool IsEnabled(LogLevel logLevel) => _isEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (_isEnabled(logLevel))
            {
                _logger.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        public IDisposable BeginScopeImpl(object state)
        {
            return BeginScopeImpl(state);
        }


        private Func<LogLevel, bool> GetFilter()
        {
            foreach (var prefix in GetKeyPrefixes(_categoryName))
            {
                LogLevel level;
                if (_settings.TryGetSwitch(prefix, out level))
                {
                    return l => l >= level;
                }
            }

            return l => true;
        }

        private IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }
                name = name.Substring(0, lastIndexOfDot);
            }
        }
    }
}

