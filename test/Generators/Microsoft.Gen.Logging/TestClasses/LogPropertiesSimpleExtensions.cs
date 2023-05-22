// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class LogPropertiesSimpleExtensions
    {
        internal class MyProps
        {
            public int P1 { get; set; }
            public int? P2 { get; set; }
            public string P3 { get; set; } = string.Empty;
            public string? P4 { get; set; }
#pragma warning disable IDE1006
            public string? @class { get; set; }
#pragma warning restore IDE1006
            public IEnumerable<int>? P5 { get; set; }
            public int[]? P6 { get; set; }
            public IDictionary<string, int>? P7 { get; set; }
        }

        [LogMethod(0, LogLevel.Debug, "{p0}")]
        public static partial void LogFunc(ILogger logger, string p0, [LogProperties] MyProps myProps);
    }
}
