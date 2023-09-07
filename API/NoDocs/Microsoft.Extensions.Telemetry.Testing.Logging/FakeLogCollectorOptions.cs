// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

public class FakeLogCollectorOptions
{
    public ISet<string> FilteredCategories { get; set; }
    public ISet<LogLevel> FilteredLevels { get; set; }
    public bool CollectRecordsForDisabledLogLevels { get; set; }
    public TimeProvider TimeProvider { get; set; }
    public Action<string>? OutputSink { get; set; }
    public Func<FakeLogRecord, string> OutputFormatter { get; set; }
    public FakeLogCollectorOptions();
}
