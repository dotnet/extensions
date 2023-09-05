// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metrics;

[SuppressMessage("Usage", "CA1801:Review unused parameters",
    Justification = "For testing emitter for classes without namespace")]
internal static partial class InstrumentsWithoutNamespace
{
    [Counter]
    public static partial NoNamespaceCounterInstrument CreatePublicCounter(Meter meter);

    [Histogram]
    public static partial NoNamespaceHistogramInstrument CreatePublicHistogram(Meter meter);
}
