// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

[ExcludeFromCodeCoverage/* (Justification = "Only used in debugger") */]
internal sealed class FakeLogCollectorDebugView
{
    private readonly FakeLogCollector _collector;

    public FakeLogCollectorDebugView(FakeLogCollector collector)
    {
        _collector = Throw.IfNull(collector);
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IReadOnlyList<FakeLogRecord> Records => _collector.GetSnapshot();
}
