// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

internal static partial class Log
{
    [LogMethod(0, LogLevel.Information,
        "Fault-injection group name: {groupName}. " +
        "Fault type: {faultType}. " +
        "Injected value: {injectedValue}. " +
        "Http content key: {httpContentKey}.")]
    public static partial void LogInjection(
        ILogger logger,
        string groupName,
        string faultType,
        string injectedValue,
        string httpContentKey);
}
