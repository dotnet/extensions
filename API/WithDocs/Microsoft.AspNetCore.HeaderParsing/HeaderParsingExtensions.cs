// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Extensions exposing HeaderParsing feature.
/// </summary>
public static class HeaderParsingExtensions
{
    /// <summary>
    /// Adds header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <returns>The same <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services);

    /// <summary>
    /// Adds header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="configuration">A delegate to setup parsing for the header.</param>
    /// <returns>The same <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, Action<HeaderParsingOptions> configuration);

    /// <summary>
    /// Adds header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="section">A configuration section.</param>
    /// <returns>The same <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> instance for chaining.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Gets the header parsing feature to access parsed header values.
    /// </summary>
    /// <param name="request">The <see cref="T:Microsoft.AspNetCore.Http.HttpRequest" /> instance.</param>
    /// <returns>The <see cref="T:Microsoft.AspNetCore.HeaderParsing.HeaderParsingFeature" /> to access parsed header values.</returns>
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
