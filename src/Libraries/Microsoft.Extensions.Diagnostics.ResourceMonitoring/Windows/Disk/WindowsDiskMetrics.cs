// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Shared.Instruments;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;

internal sealed class WindowsDiskMetrics
{
    public WindowsDiskMetrics(IMeterFactory meterFactory)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        // We don't dispose the meter because IMeterFactory handles that
        // It's a false-positive, see: https://github.com/dotnet/roslyn-analyzers/issues/6912.
        // Related documentation: https://github.com/dotnet/docs/pull/37170
        var meter = meterFactory.Create(ResourceUtilizationInstruments.MeterName);
#pragma warning restore CA2000 // Dispose objects before losing scope
    }
}
