// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal static partial class TagNameExtensions
    {
        [LoggerMessage(LogLevel.Warning)]
        internal static partial void M0(ILogger logger, [TagName("TN1")] int p0);

        [LoggerMessage(LogLevel.Warning, Message = "{foo.bar}")]
        internal static partial void M1(ILogger logger, [TagName("foo.bar")] int p0);
    }
}
