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

    public partial class LoggerInProtectedFieldTestClass
    {
        protected readonly ILogger Logger;

        public LoggerInProtectedFieldTestClass(ILogger logger)
        {
            Logger = logger;
        }

        [LoggerMessage(1, LogLevel.Debug, "M0 {p0}")]
        public partial void M0(string p0);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used in generated code")]
    public partial class PrivateLoggerInNullablePropertyDerivedTestClass : LoggerInNullablePropertyTestClass
    {
        private readonly ILogger _logger;

        public PrivateLoggerInNullablePropertyDerivedTestClass(ILogger logger)
        {
            _logger = logger;
        }

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
        public LoggerInProtectedFieldDerivedTestClass(ILogger logger)
            : base(logger)
        {
        }

        [LoggerMessage(1, LogLevel.Debug, "M1 {p0}")]
        public partial void M1(string p0);
    }
}
