// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Utility type to support generated logging methods.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class LoggerMessageHelper
{
    /// <summary>
    /// Gets a thread-local instance of this type.
    /// </summary>
    public static LoggerMessageState ThreadLocalState { get; }

    /// <summary>
    /// Enumerates an enumerable into a string.
    /// </summary>
    /// <param name="enumerable">The enumerable object.</param>
    /// <returns>
    /// A string representation of the enumerable.
    /// </returns>
    public static string Stringify(IEnumerable? enumerable);

    /// <summary>
    /// Enumerates an enumerable of key/value pairs into a string.
    /// </summary>
    /// <typeparam name="TKey">Type of keys.</typeparam>
    /// <typeparam name="TValue">Type of values.</typeparam>
    /// <param name="enumerable">The enumerable object.</param>
    /// <returns>
    /// A string representation of the enumerable.
    /// </returns>
    public static string Stringify<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? enumerable);
}
