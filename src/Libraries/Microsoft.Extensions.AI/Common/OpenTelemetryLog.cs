// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI;

/// <summary>Shared log methods for OpenTelemetry instrumentation classes.</summary>
internal static partial class OpenTelemetryLog
{
    [LoggerMessage(
        EventName = "gen_ai.client.operation.exception",
        Level = LogLevel.Warning,
        Message = "gen_ai.client.operation.exception")]
    internal static partial void OperationException(ILogger logger, Exception error);
}
