// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Utility type to support generated logging methods.
/// </summary>
/// <remarks>
/// This type is not intended to be directly invoked by application code,
/// it is intended to be invoked by generated logging method code.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LogMethodHelper : List<KeyValuePair<string, object?>>, ITagCollector, IEnrichmentTagCollector, IResettable
{
    /// <summary>
    /// Gets or sets the name of the logging method parameter for which to collect tags.
    /// </summary>
    public string ParameterName { get; set; }

    /// <summary>
    /// Gets log define options configured to skip the log level enablement check.
    /// </summary>
    public static LogDefineOptions SkipEnabledCheckOptions { get; }

    /// <inheritdoc />
    public void Add(string tagName, object? tagValue);

    /// <inheritdoc />
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public void Add(string tagName, object? tagValue, DataClassification classification);

    /// <summary>
    /// Resets state of this container as described in <see cref="M:Microsoft.Extensions.ObjectPool.IResettable.TryReset" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the object successfully reset and can be reused.
    /// </returns>
    public bool TryReset();

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

    /// <summary>
    /// Gets an instance of a helper from the global pool.
    /// </summary>
    /// <returns>A usable instance.</returns>
    public static LogMethodHelper GetHelper();

    /// <summary>
    /// Returns a helper instance to the global pool.
    /// </summary>
    /// <param name="helper">The helper instance.</param>
    public static void ReturnHelper(LogMethodHelper helper);

    public LogMethodHelper();
}
