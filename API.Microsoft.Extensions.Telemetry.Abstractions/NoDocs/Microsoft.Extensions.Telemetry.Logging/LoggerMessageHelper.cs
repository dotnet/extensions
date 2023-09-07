// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Telemetry.Logging;

[EditorBrowsable(EditorBrowsableState.Never)]
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class LoggerMessageHelper
{
    public static LoggerMessageState ThreadLocalState { get; }
    public static string Stringify(IEnumerable? enumerable);
    public static string Stringify<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? enumerable);
}
