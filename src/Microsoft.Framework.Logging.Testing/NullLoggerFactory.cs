// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.Logging.Testing
{
    public class NullLoggerFactory : ILoggerFactory
    {
        public static readonly NullLoggerFactory Instance = new NullLoggerFactory();

        public LogLevel MinimumLevel { get; set; } = LogLevel.Verbose;

        public ILogger CreateLogger(string name)
        {
            return NullLogger.Instance;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
}