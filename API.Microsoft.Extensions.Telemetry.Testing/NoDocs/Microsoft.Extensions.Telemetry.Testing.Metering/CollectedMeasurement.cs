// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Testing.Metering;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
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
