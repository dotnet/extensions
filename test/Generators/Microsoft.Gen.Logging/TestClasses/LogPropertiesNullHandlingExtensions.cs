// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class LogPropertiesNullHandlingExtensions
    {
        internal class MyProps
        {
            public string P0 { get; set; } = string.Empty;
            public string? P1 { get; set; }
            public int P2 { get; set; }
            public int? P3 { get; set; }

            [PrivateData]
            public string? P4 { get; set; }
        }

        [LogMethod(LogLevel.Debug)]
        public static partial void M0(ILogger logger, [LogProperties] MyProps p);

        [LogMethod(LogLevel.Debug)]
        public static partial void M1(ILogger logger, [LogProperties(SkipNullProperties = true)] MyProps p);
    }
}
