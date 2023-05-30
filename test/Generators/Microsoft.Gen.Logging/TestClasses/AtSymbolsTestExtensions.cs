// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class AtSymbolsTestExtensions
    {
        [LogMethod(0, LogLevel.Information, "M0 {event}")]
        internal static partial void M0(ILogger logger, string @event);

        [LogMethod(1, LogLevel.Information, "M1 {event}")]
        internal static partial void M1(ILogger logger, IRedactorProvider redactorProvider, [PrivateData] string @event);

        [LogMethod(int.MaxValue, "M2 {Event}")]
        internal static partial void M2(ILogger logger, LogLevel level, string @event);

        // And support with property logging
        [LogMethod(3, "M3")]
        internal static partial void M3(ILogger logger, LogLevel level, [LogProperties] ClassToLog @event);

        [LogMethod(LogLevel.Information, "M4 {class}")]
        internal static partial void M4(ILogger logger, string @class);
    }
}
