// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.FaultInjection;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Builder class that inherits <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptionsBuilder" /> to provide options configuration methods for
/// <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" />, <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionExceptionOptions" /> and <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.Internal.HttpContentOptions" />.
/// </summary>
public class HttpFaultInjectionOptionsBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.HttpFaultInjectionOptionsBuilder" /> class.
    /// </summary>
    /// <param name="services">The services collection.</param>
    public HttpFaultInjectionOptionsBuilder(IServiceCollection services);

    /// <summary>
    /// Configures default <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" />.
    /// </summary>
    /// <returns>
    /// The builder object itself so that additional calls can be chained.
    /// </returns>
    public HttpFaultInjectionOptionsBuilder Configure();

    /// <summary>
    /// Configures <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" /> through
    /// the provided <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" />.
    /// </summary>
    /// <param name="section">
    /// The configuration section to bind to <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" />.
    /// </param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    public HttpFaultInjectionOptionsBuilder Configure(IConfiguration section);

    /// <summary>
    /// Configures <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" /> through
    /// the provided configure.
    /// </summary>
    /// <param name="configureOptions">
    /// The function to be registered to configure <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" />.
    /// </param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    public HttpFaultInjectionOptionsBuilder Configure(Action<FaultInjectionOptions> configureOptions);

    /// <summary>
    /// Add an exception instance to <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionExceptionOptions" />.
    /// </summary>
    /// <param name="key">The identifier for the exception instance to be added.</param>
    /// <param name="exception">The exception instance to be added.</param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="exception" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="key" /> is an empty string or <see langword="null" />.
    /// </exception>
    public HttpFaultInjectionOptionsBuilder AddException(string key, Exception exception);

    /// <summary>
    /// Adds a <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.Internal.HttpContentOptions" /> with the provided <see cref="T:System.Net.Http.HttpContent" />.
    /// </summary>
    /// <param name="key">The identifier for the options instance to be added.</param>
    /// <param name="content">The http content.</param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    public HttpFaultInjectionOptionsBuilder AddHttpContent(string key, HttpContent content);
}
