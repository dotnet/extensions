// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.Probes;

internal static partial class Log
{
    [LoggerMessage(LogLevel.Error, "Error updating health status through TCP endpoint")] 
    public static partial void SocketExceptionCaughtTcpEndpoint(this ILogger logger, Exception e);
}
