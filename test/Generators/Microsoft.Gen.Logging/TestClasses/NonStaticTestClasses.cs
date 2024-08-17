// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

#pragma warning disable SA1402

namespace TestClasses
{
    public partial class LoggerInPropertyTestClass
    {
        public ILogger Logger { get; set; } = null!;

        [LoggerMessage(0, LogLevel.Debug, "M0 {p0}")]
        public partial void M0(string p0);
    }

    public partial class LoggerInNullablePropertyTestClass
    {
        public ILogger? Logger { get; set; }

        [LoggerMessage(0, LogLevel.Debug, "M0 {p0}")]
        public partial void M0(string p0);
    }

    public partial class GenericLoggerInPropertyTestClass
    {
        public ILogger<int> Logger { get; set; } = null!;

        [LoggerMessage(0, LogLevel.Debug, "M0 {p0}")]
        public partial void M0(string p0);
    }

    public partial class LoggerInPropertyDerivedTestClass : LoggerInPropertyTestClass
    {
        [LoggerMessage(1, LogLevel.Debug, "M1 {p0}")]
        public partial void M1(string p0);
    }

    public partial class LoggerInNullablePropertyDerivedTestClass : LoggerInNullablePropertyTestClass
    {
        [LoggerMessage(1, LogLevel.Debug, "M1 {p0}")]
        public partial void M1(string p0);
    }

    // The following classes are not used in any tests, but they validate that the generation is working correctly.

    public partial class LoggerInProtectedFieldTestClass
    {
        protected readonly ILogger Logger = null!;

        [LoggerMessage(1, LogLevel.Debug, "M0 {p0}")]
        public partial void M0(string p0);
    }

    public partial class PrivateLoggerInPropertyDerivedTestClass : LoggerInPropertyTestClass
    {
#pragma warning disable IDE0051 // Remove unused private members. Used to test generation.
        private readonly ILogger _logger = null!;
#pragma warning restore IDE0051 // Remove unused private members

        [LoggerMessage(1, LogLevel.Debug, "M1 {p0}")]
        public partial void M1(string p0);
    }

    public partial class GenericLoggerInPropertyDerivedTestClass : GenericLoggerInPropertyTestClass
    {
        [LoggerMessage(1, LogLevel.Debug, "M1 {p0}")]
        public partial void M1(string p0);
    }

    public partial class LoggerInProtectedFieldDerivedTestClass : LoggerInProtectedFieldTestClass
    {
        [LoggerMessage(1, LogLevel.Debug, "M1 {p0}")]
        public partial void M1(string p0);
    }
}
