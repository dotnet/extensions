// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging.Testing;

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
