// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HeaderParsing;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extensions for header parsing.
/// </summary>
public static class HeaderParsingHttpRequestExtensions
{
    /// <summary>
    /// Gets the header parsing feature to access parsed header values.
    /// </summary>
    /// <param name="request">The <see cref="T:Microsoft.AspNetCore.Http.HttpRequest" /> instance.</param>
    /// <returns>The value of <paramref name="request" />.</returns>
    public static HeaderParsingFeature GetHeaderParsing(this HttpRequest request);

    /// <summary>
    /// Tries to get a header value if it exists and can be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="request">The <see cref="T:Microsoft.AspNetCore.Http.HttpRequest" /> instance.</param>
    /// <param name="header">The header to parse.</param>
    /// <param name="value">A resulting value.</param>
    /// <returns><see langword="true" /> if the header value was successfully fetched parsed.</returns>
    public static bool TryGetHeaderValue<T>(this HttpRequest request, HeaderKey<T> header, [NotNullWhen(true)] out T? value) where T : notnull;

    /// <summary>
    /// Tries to get a header value if it exists and can be parsed.
    /// </summary>
    /// <typeparam name="T">The type of the header value.</typeparam>
    /// <param name="request">The <see cref="T:Microsoft.AspNetCore.Http.HttpRequest" /> instance.</param>
    /// <param name="header">The header to parse.</param>
    /// <param name="value">A resulting value.</param>
    /// <param name="result">Details on the parsing operation.</param>
    /// <returns><see langword="true" /> if the header value was successfully fetched parsed.</returns>
    public static bool TryGetHeaderValue<T>(this HttpRequest request, HeaderKey<T> header, [NotNullWhen(true)] out T? value, out ParsingResult result) where T : notnull;
}
