// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class InParameterTestExtensions
    {
        internal struct S
        {
            public override string ToString() => "Hello from S";
        }

        [LogMethod(0, LogLevel.Information, "M0 {s}")]
        internal static partial void M0(ILogger logger, in S s);
    }
}
