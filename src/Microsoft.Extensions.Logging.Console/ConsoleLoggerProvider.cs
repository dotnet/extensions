// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Console
{
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly bool _includeScopes;

        public ConsoleLoggerProvider(Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            _filter = filter;
            _includeScopes = includeScopes;
        }

        public ILogger CreateLogger(string name)
        {
            return new ConsoleLogger(name, _filter, _includeScopes);
        }

        public void Dispose()
        {
        }
    }
}
