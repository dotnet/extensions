// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Filter.Internal
{
    public class FilterLoggerProvider : ILoggerProvider
    {
        private ILoggerProvider _innerLoggerProvider;
        private IFilterLoggerSettings _settings;

        public FilterLoggerProvider(ILoggerProvider innerLoggerProvider, IFilterLoggerSettings settings)
        {
            _innerLoggerProvider = innerLoggerProvider;
            _settings = settings;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = _innerLoggerProvider.CreateLogger(categoryName);
            var wrappedLogger = new FilterLogger(logger, categoryName, _settings);
            return wrappedLogger;
        }

        public void Dispose()
        {
            _innerLoggerProvider.Dispose();
        }
    }
}