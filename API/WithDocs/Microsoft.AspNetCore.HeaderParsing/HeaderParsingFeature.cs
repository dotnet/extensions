// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Keeps header parsing state and provides parsing features.
/// </summary>
public sealed class HeaderParsingFeature
{
    /// <summary>
    /// Tries to get a header value if it exists and can be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="header">The header to parse.</param>
    /// <param name="value">A resulting value.</param>
    /// <returns><see langword="true" /> if the header value was successfully fetched parsed.</returns>
    public bool TryGetHeaderValue<T>(HeaderKey<T> header, [NotNullWhen(true)] out T? value) where T : notnull;

    /// <summary>
    /// Tries to get a header value if it exists and can be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="header">The header to parse.</param>
    /// <param name="value">A resulting value.</param>
    /// <param name="result">Details on the parsing operation.</param>
    /// <returns><see langword="true" /> if the header value was successfully fetched parsed.</returns>
    public bool TryGetHeaderValue<T>(HeaderKey<T> header, [NotNullWhen(true)] out T? value, out ParsingResult result) where T : notnull;
}
