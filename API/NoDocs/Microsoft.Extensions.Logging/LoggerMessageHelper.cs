// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Extensions.Logging;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class LoggerMessageHelper
{
    public static LoggerMessageState ThreadLocalState { get; }
    public static string Stringify(IEnumerable? enumerable);
    public static string Stringify<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? enumerable);
}
