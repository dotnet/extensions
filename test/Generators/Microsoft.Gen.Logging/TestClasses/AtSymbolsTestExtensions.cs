// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;

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

        [LoggerMessage(1, LogLevel.Information, "M1 {@myevent1}")]
        internal static partial void M1(ILogger logger, [PrivateData] string @myevent1);

        [LoggerMessage(Message = "UseAtSymbol3, {@myevent2} {otherevent}", EventId = 2)]
        public static partial void UseAtSymbol3(ILogger logger, LogLevel level, string @myevent2, int otherevent);

        [LoggerMessage(Message = "UseAtSymbol4 with error, {@myevent3} {otherevent}", EventId = 3)]
        public static partial void UseAtSymbol4(ILogger logger, LogLevel level, string @myevent3, int otherevent, System.Exception ex);

        [LoggerMessage("M2 {Event}")]
        internal static partial void M2(ILogger logger, LogLevel level, string @event);

        [LoggerMessage("M3")]
        internal static partial void M3(ILogger logger, LogLevel level, [LogProperties] SpecialNames @event);

        [LoggerMessage(LogLevel.Information, "M4 {class}")]
        internal static partial void M4(ILogger logger, string @class);

        [LoggerMessage("M5")]
        internal static partial void M5(ILogger logger, LogLevel level, [LogProperties(OmitReferenceName = true)] SpecialNames @event);

        [LoggerMessage(LogLevel.Information, "M6 class {class}")]
        internal static partial void M6(ILogger logger, string @class);

        [LoggerMessage(LogLevel.Information, "M7 param {@param}")]
        internal static partial void M7(ILogger logger, string param);
    }
}
