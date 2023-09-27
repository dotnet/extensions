// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to register extended HTTP client logging features.
/// </summary>
public static class HttpClientLoggingHttpClientBuilderExtensions
{
    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.Http.Logging.IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="T:System.Net.Http.HttpClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="M:Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions.AddDefaultLogger(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder)" />.
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException">Argument <paramref name="builder" /> is <see langword="null" />.</exception>
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder);

    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.Http.Logging.IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="T:System.Net.Http.HttpClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="section">The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> to use for configuring <see cref="T:Microsoft.Extensions.Http.Logging.LoggingOptions" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="M:Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions.AddDefaultLogger(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder)" />.
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Adds an <see cref="T:Microsoft.Extensions.Http.Logging.IHttpClientAsyncLogger" /> to emit logs for outgoing requests for a named <see cref="T:System.Net.Http.HttpClient" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="configure">The delegate to configure <see cref="T:Microsoft.Extensions.Http.Logging.LoggingOptions" /> with.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <remarks>
    /// All other loggers are removed - including the default one, registered via <see cref="M:Microsoft.Extensions.DependencyInjection.HttpClientBuilderExtensions.AddDefaultLogger(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder)" />.
    /// </remarks>
    /// <exception cref="T:System.ArgumentNullException">Any of the arguments is <see langword="null" />.</exception>
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure);
}
