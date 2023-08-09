// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class AttributeTestExtensions
    {
        [LoggerMessage(0, LogLevel.Debug, "M0 {p0}")]
        public static partial void M0(ILogger logger, [In] string p0);

        [LoggerMessage(1, LogLevel.Debug, "M1 {p0} {p1}")]
        public static partial void M1(ILogger logger, [PrivateData] string p0, string p1);

        [LoggerMessage(2, LogLevel.Debug, "M2 {p0} {p1}")]
        public static partial void M2(ILogger logger, [PrivateData] string p0, [In] string p1);

        [LoggerMessage(3, LogLevel.Debug, "M3 {p0} {p1} {p2} {p3}")]
        public static partial void M3(
            ILogger logger,
            [PrivateData] string p0,
            [PrivateData] string p1,
            [PrivateData] string p2,
            [PrivateData] string p3);

        [LoggerMessage(4, LogLevel.Debug, "M4 {p0} {p1} {p2}")]
        public static partial void M4(
            ILogger logger,
            [PrivateData] string p0,
            [PrivateData] string p1,
            [PrivateData] string p2);

        [LoggerMessage(5, LogLevel.Debug, "M5 {p0} {p1} {p2} {p3} {p4} {p5} {p6} {p7} {p8} {p9} {p10}")]
        [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "Testing.")]
        public static partial void M5(
            ILogger logger,
            [PrivateData] string p0,
            [PrivateData] string p1,
            [PrivateData] string p2,
            [PrivateData] string p3,
            [PrivateData] string p4,
            [PrivateData] string p5,
            [PrivateData] string p6,
            [PrivateData] string p7,
            [PublicData] string p8,
            [PublicData] string p9,
            [PublicData] string p10);

        // Parameterless ctor:
        [LoggerMessage]
        public static partial void M6(ILogger logger, LogLevel level,
            [PrivateData] string p0, string p1);

        [LoggerMessage]
        public static partial void M7(ILogger logger, LogLevel level,
            [PrivateData] string p0, string p1);

        [LoggerMessage(8, LogLevel.Debug, "M8 {p0}")]
        public static partial void M8(ILogger logger, [PrivateData] int p0);

        [LoggerMessage(9, LogLevel.Debug, "M9 {p0}")]
        public static partial void M9(ILogger logger, [PrivateData] CustomToStringTestClass p0);
    }
}
