// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class OverloadsTestExtensions
    {
        [LoggerMessage(0, LogLevel.Information, "M0 {v}", EventName = "One")]
        internal static partial void M0(ILogger logger, int v);

        [LoggerMessage(1, LogLevel.Information, "M0 {v}", EventName = "Two")]
        internal static partial void M0(ILogger logger, string v);
    }
}
