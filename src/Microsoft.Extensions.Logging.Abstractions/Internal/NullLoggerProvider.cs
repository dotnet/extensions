// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Abstractions.Internal
{
    public class NullLoggerProvider : ILoggerProvider
    {
        public static NullLoggerProvider Instance { get; } = new NullLoggerProvider();

        private NullLoggerProvider()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }

        public void Dispose()
        {
        }
    }
}
