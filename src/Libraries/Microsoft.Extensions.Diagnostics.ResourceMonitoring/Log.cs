﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

[SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "Generators.")]
[SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used", Justification = "Generators.")]
internal static partial class Log
{
    [LoggerMessage(1, LogLevel.Error, "Unable to gather utilization statistics.")]
    public static partial void HandledGatherStatisticsException(ILogger logger, Exception e);

    [LoggerMessage(2, LogLevel.Error, "Publisher `{Publisher}` was unable to publish utilization statistics.")]
    public static partial void HandlePublishUtilizationException(ILogger logger, Exception e, string publisher);
}
