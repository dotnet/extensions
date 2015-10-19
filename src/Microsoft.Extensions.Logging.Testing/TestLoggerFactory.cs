// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Testing
{
    public class TestLoggerFactory : ILoggerFactory
    {
        private readonly TestSink _sink;
        private readonly bool _enabled;

        public TestLoggerFactory(TestSink sink, bool enabled)
        {
            _sink = sink;
            _enabled = enabled;
        }

        public LogLevel MinimumLevel { get; set; } = LogLevel.Verbose;

        public ILogger CreateLogger(string name)
        {
            return new TestLogger(name, _sink, _enabled);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}