// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class NullableTestExtensions
    {
        [LogMethod(0, LogLevel.Debug, "M0 {p0}")]
        internal static partial void M0(ILogger logger, string? p0);

        [LogMethod(1, LogLevel.Debug, "M1 {p0}")]
        internal static partial void M1(ILogger logger, int? p0);

        [LogMethod(3, LogLevel.Debug, "M3 {p0}")]
        internal static partial void M3(ILogger logger, [PrivateData] string? p0);

#pragma warning disable S107 // Methods should not have too many parameters
        [LogMethod(4, LogLevel.Debug, "M4 {p0} {p1} {p2} {p3} {p4} {p5} {p6} {p7} {p8}")]
        internal static partial void M4(ILogger logger, int? p0, int? p1, int? p2, int? p3, int? p4, int? p5, int? p6, int? p7, int? p8);

        [LogMethod(5, LogLevel.Debug, "M5 {p0} {p1} {p2} {p3} {p4} {p5} {p6} {p7} {p8}")]
        internal static partial void M5(ILogger logger, string? p0, string? p1, string? p2, string? p3, string? p4, string? p5, string? p6, string? p7, string? p8);
#pragma warning restore S107 // Methods should not have too many parameters

        [LogMethod(6, LogLevel.Debug, "M6 {p0}")]
        internal static partial void M6(ILogger? logger, [PrivateData] string? p0);
    }
}
