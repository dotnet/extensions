// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Filter.Internal
{
    public class FilterLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _innerLoggerFactory;
        private readonly IFilterLoggerSettings _settings;

        public FilterLoggerFactory(ILoggerFactory innerLoggerFactory, IFilterLoggerSettings settings)
        {
            _innerLoggerFactory = innerLoggerFactory;
            _settings = settings;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            var wrappedProvider = new FilterLoggerProvider(provider, _settings);
            _innerLoggerFactory.AddProvider(wrappedProvider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _innerLoggerFactory.CreateLogger(categoryName);
        }

        public void Dispose()
        {
            // Do not dispose the inner logger factory as this filter logger factory's only responsibility is to
            // wrap the logger providers. Calling dispose on the inner logger factory can cause dispose to be called
            // immediately after the providers are added.
        }
    }
}
