// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;

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
            public NonFormattable P4 { get; set; }

            [PrivateData]
            public string P5 { get; set; } = string.Empty;

            [PrivateData]
            public string? P6 { get; set; }

            [PrivateData]
            public int P7 { get; set; }

            [PrivateData]
            public int? P8 { get; set; }

            [PrivateData]
            public NonFormattable P9 { get; set; }

            public List<int>? P10 { get; set; }
        }

        [LoggerMessage(LogLevel.Debug)]
        public static partial void M0(ILogger logger, [LogProperties] MyProps p);

        [LoggerMessage(LogLevel.Debug)]
        public static partial void M1(ILogger logger, [LogProperties(SkipNullProperties = true)] MyProps p);
    }
}
