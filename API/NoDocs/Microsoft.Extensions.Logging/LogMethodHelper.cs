// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.Logging;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LogMethodHelper : List<KeyValuePair<string, object?>>, ITagCollector, IEnrichmentTagCollector, IResettable
{
    public string ParameterName { get; set; }
    public static LogDefineOptions SkipEnabledCheckOptions { get; }
    public void Add(string tagName, object? tagValue);
    public void Add(string tagName, object? tagValue, DataClassification classification);
    public bool TryReset();
    public static string Stringify(IEnumerable? enumerable);
    public static string Stringify<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? enumerable);
    public static LogMethodHelper GetHelper();
    public static void ReturnHelper(LogMethodHelper helper);
    public LogMethodHelper();
}
