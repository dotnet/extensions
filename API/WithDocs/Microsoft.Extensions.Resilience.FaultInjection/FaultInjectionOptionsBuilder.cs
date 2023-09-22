// Assembly 'Microsoft.Extensions.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Builder class to provide options configuration methods for
/// <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" /> and <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionExceptionOptions" />.
/// </summary>
public class FaultInjectionOptionsBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptionsBuilder" /> class.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    public FaultInjectionOptionsBuilder(IServiceCollection services);

    /// <summary>
    /// Configures default <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" />.
    /// </summary>
    /// <returns>
    /// The builder object itself so that additional calls can be chained.
    /// </returns>
    public FaultInjectionOptionsBuilder Configure();

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
    public FaultInjectionOptionsBuilder Configure(IConfiguration section);

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
    public FaultInjectionOptionsBuilder Configure(Action<FaultInjectionOptions> configureOptions);

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
    public FaultInjectionOptionsBuilder AddException(string key, Exception exception);

    /// <summary>
    /// Add a custom result object instance to <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionCustomResultOptions" />.
    /// </summary>
    /// <param name="key">The identifier for the custom result object instance to be added.</param>
    /// <param name="customResult">The custom result object instance to be added.</param>
    /// <returns>The builder object itself so that additional calls can be chained.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="customResult" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="key" /> is an empty string or <see langword="null" />.
    /// </exception>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public FaultInjectionOptionsBuilder AddCustomResult(string key, object customResult);
}
