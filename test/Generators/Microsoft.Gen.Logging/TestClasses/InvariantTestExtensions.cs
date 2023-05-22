// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace TestClasses
{
    internal static partial class InvariantTestExtensions
    {
        [LogMethod(0, LogLevel.Debug, "M0 {p0}")]
        public static partial void M0(ILogger logger, DateTime p0);
    }
}
