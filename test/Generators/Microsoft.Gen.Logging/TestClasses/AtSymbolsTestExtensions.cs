// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

#pragma warning disable IDE1006 // Naming Styles

namespace TestClasses
{
    internal static partial class AtSymbolsTestExtensions
    {
        public class SpecialNames
        {
            public int @class { get; set; }
        }

        [LoggerMessage(0, LogLevel.Information, "M0 {event}")]
        internal static partial void M0(ILogger logger, string @event);

        [LoggerMessage(1, LogLevel.Information, "M1 {event}")]
        internal static partial void M1(ILogger logger, [PrivateData] string @event);

        [LoggerMessage("M2 {Event}")]
        internal static partial void M2(ILogger logger, LogLevel level, string @event);

        [LoggerMessage("M3")]
        internal static partial void M3(ILogger logger, LogLevel level, [LogProperties] SpecialNames @event);

        [LoggerMessage(LogLevel.Information, "M4 {class}")]
        internal static partial void M4(ILogger logger, string @class);

        [LoggerMessage("M5")]
        internal static partial void M5(ILogger logger, LogLevel level, [LogProperties(OmitReferenceName = true)] SpecialNames @event);
    }
}
