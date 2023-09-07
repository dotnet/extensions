// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class DataClassificationTestExtensions
    {
        [PublicData]
        public class C1
        {
            public string? P1 { get; set; }

            [PrivateData]
            public string? P2 { get; set; }

            public override string ToString() => $"{P1}+{P2}";
        }

        [LoggerMessage(1, LogLevel.Error, "M0 {p1}")]
        public static partial void M0(ILogger logger, [PublicData] string p1);

        [LoggerMessage(2, LogLevel.Error, "M1 {p1} {p2}")]
        public static partial void M1(ILogger logger, [PublicData, PrivateData] string p1, [PublicData] string p2);

        [LoggerMessage(3, LogLevel.Error, "M2")]
        public static partial void M2(ILogger logger, [LogProperties] C1 p1, [PrivateData] C1 p2);
    }
}
