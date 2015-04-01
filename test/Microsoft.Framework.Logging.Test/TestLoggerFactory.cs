// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Logging.Test
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

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _sink, _enabled);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}