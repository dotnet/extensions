// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Logging;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LoggerMessageState : IEnrichmentTagCollector, IReadOnlyList<KeyValuePair<string, object?>>, IEnumerable<KeyValuePair<string, object?>>, IEnumerable, IReadOnlyCollection<KeyValuePair<string, object?>>, ITagCollector
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ClassifiedTag
    {
        public string Name { get; }
        public object? Value { get; }
        public DataClassification Classification { get; }
        public ClassifiedTag(string name, object? value, DataClassification classification);
    }
    public KeyValuePair<string, object?>[] TagArray { get; }
    public KeyValuePair<string, object?>[] RedactedTagArray { get; }
    public ClassifiedTag[] ClassifiedTagArray { get; }
    public int TagsCount { get; }
    public int ClassifiedTagsCount { get; }
    public KeyValuePair<string, object?> this[int index] { get; }
    public string TagNamePrefix { get; set; }
    public int ReserveTagSpace(int count);
    public int ReserveClassifiedTagSpace(int count);
    public void AddTag(string name, object? value);
    public void AddClassifiedTag(string name, object? value, DataClassification classification);
    public void Clear();
    public override string ToString();
    public LoggerMessageState();
}
