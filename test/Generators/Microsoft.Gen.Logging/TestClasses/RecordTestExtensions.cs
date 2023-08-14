// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace TestClasses
{
    internal partial record RecordTestExtensions(string Name, string Address)
    {
        [LoggerMessage(12, LogLevel.Debug, "M0")]
        public static partial void M0(ILogger logger);
    }
}
