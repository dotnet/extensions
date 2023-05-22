// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Telemetry.Testing.Logging.Test;

internal static partial class TestLog
{
    [LogMethod(0, LogLevel.Error, "Hello {name}")]
    public static partial void Hello(this ILogger logger, string name);
}
