// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Metrics.Testing;

[DebuggerDisplay("{DebuggerToString(),nq}")]
public sealed class CollectedMeasurement<T> where T : struct
{
    public T Value { get; }
    public DateTimeOffset Timestamp { get; }
    public IReadOnlyDictionary<string, object?> Tags { get; }
    public bool ContainsTags(params KeyValuePair<string, object?>[] tags);
    public bool ContainsTags(params string[] tags);
    public bool MatchesTags(params KeyValuePair<string, object?>[] tags);
    public bool MatchesTags(params string[] tags);
}
