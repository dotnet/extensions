// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Logging;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LogMethodHelper : List<KeyValuePair<string, object?>>, ITagCollector, IEnrichmentTagCollector, IResettable
{
    public string ParameterName { get; set; }
    public static LogDefineOptions SkipEnabledCheckOptions { get; }
    public void Add(string tagName, object? tagValue);
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public void Add(string tagName, object? tagValue, DataClassification classification);
    public bool TryReset();
    public static string Stringify(IEnumerable? enumerable);
    public static string Stringify<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? enumerable);
    public static LogMethodHelper GetHelper();
    public static void ReturnHelper(LogMethodHelper helper);
    public LogMethodHelper();
}
