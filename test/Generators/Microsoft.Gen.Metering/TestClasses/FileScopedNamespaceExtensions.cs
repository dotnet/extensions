// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN_4_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

namespace TestClasses;

[SuppressMessage("Readability", "R9A046:Source generated metrics (fast metrics) should be located in 'Metric' class",
    Justification = "Metering generator tests")]
internal static partial class FileScopedExtensions
{
    [Counter]
    public static partial FileScopedNamespaceCounter CreateCounter(Meter meter);

    [Counter<double>]
    public static partial FileScopedNamespaceGenericDoubleCounter CreateGenericDoubleCounter(Meter meter);
}

#endif
