// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    public partial class TestInstances
    {
#pragma warning disable IDE0052
        private readonly ILogger _myLogger;
#pragma warning restore IDE0052

        public TestInstances(ILogger logger)
        {
            _myLogger = logger;
        }

        [LoggerMessage(0, LogLevel.Error, "M0")]
        public partial void M0();

        [LoggerMessage(1, LogLevel.Trace, "M1 {p1}")]
        public partial void M1(string p1);

        [LoggerMessage]
        public partial void M2(LogLevel level, string p1);
    }
}
