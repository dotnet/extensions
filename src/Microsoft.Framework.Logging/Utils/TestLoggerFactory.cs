// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.Logging
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

        public ILogger Create(string name)
        {
            return new TestLogger(name, _sink, _enabled);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
}