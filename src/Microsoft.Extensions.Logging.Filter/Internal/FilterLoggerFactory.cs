using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.Filter.Internal
{
    public class FilterLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IFilterLoggerSettings _settings;

        public FilterLoggerFactory(
            ILoggerFactory loggerFactory, 
            IFilterLoggerSettings settings)
        {
            _loggerFactory = loggerFactory;
            _settings = settings;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            var wrappedProvider = new FilterLoggerProvider(provider, _settings);
            _loggerFactory.AddProvider(wrappedProvider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }

        public void Dispose()
        {
        }
    }
}
