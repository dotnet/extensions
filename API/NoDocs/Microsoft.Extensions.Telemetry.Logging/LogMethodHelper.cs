// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LogMethodHelper : List<KeyValuePair<string, object?>>, ILogPropertyCollector, IEnrichmentPropertyBag, IResettable
{
    public string ParameterName { get; set; }
    public static LogDefineOptions SkipEnabledCheckOptions { get; }
    public void Add(string propertyName, object? propertyValue);
    public bool TryReset();
    public static string Stringify(IEnumerable? enumerable);
    public static string Stringify<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? enumerable);
    public static LogMethodHelper GetHelper();
    public static void ReturnHelper(LogMethodHelper helper);
    public LogMethodHelper();
}
