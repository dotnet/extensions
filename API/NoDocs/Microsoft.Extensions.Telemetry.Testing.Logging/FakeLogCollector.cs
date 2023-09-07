// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

[DebuggerDisplay("Count = {Count}, LatestRecord = {LatestRecord}")]
[DebuggerTypeProxy(typeof(FakeLogCollectorDebugView))]
public class FakeLogCollector
{
    public FakeLogRecord LatestRecord { get; }
    public int Count { get; }
    public FakeLogCollector(IOptions<FakeLogCollectorOptions> options);
    public FakeLogCollector();
    public static FakeLogCollector Create(FakeLogCollectorOptions options);
    public void Clear();
    public IReadOnlyList<FakeLogRecord> GetSnapshot(bool clear = false);
}
