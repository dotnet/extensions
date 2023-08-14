// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal readonly partial struct StructTestExtensions
    {
        [LoggerMessage(0, LogLevel.Trace, "M0")]
        public static partial void M0(ILogger logger);
    }
}
