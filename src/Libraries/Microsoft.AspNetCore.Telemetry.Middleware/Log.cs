// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Telemetry;

internal static partial class Log
{
    [LoggerMessage(0, LogLevel.Warning, "Enricher failed: {Enricher}.")]
    internal static partial void EnricherFailed(this ILogger logger, Exception exception, string enricher);
}
