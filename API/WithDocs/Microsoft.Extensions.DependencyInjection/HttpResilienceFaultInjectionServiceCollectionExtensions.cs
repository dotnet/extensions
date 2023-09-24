// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Resilience.FaultInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for Fault-Injection library specifically for HttpClient usages.
/// </summary>
public static class HttpResilienceFaultInjectionServiceCollectionExtensions
{
    /// <summary>
    /// Registers default implementations for <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IFaultInjectionOptionsProvider" />, <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IChaosPolicyFactory" /> and <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.IHttpClientChaosPolicyFactory" />;
    /// adds fault-injection policies to all <see cref="T:System.Net.Http.HttpClient" />.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services);

    /// <summary>
    /// Configures <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" /> and registers default implementations for <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IFaultInjectionOptionsProvider" />,
    /// <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IChaosPolicyFactory" /> and <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.IHttpClientChaosPolicyFactory" />;
    /// adds fault-injection policies to all <see cref="T:System.Net.Http.HttpClient" />.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="section">The configuration section to bind to <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services, IConfiguration section);

    /// <summary>
    /// Calls the given action to configure options with <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.HttpFaultInjectionOptionsBuilder" /> and registers default implementations for
    /// <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IFaultInjectionOptionsProvider" />, <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IChaosPolicyFactory" /> and <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.IHttpClientChaosPolicyFactory" />;
    /// adds fault-injection policies to all <see cref="T:System.Net.Http.HttpClient" />.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configure">Action to configure options with <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.HttpFaultInjectionOptionsBuilder" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// If the default instance of <see cref="T:System.Net.Http.IHttpClientFactory" /> is used, this method also adds a
    /// chaos policy handler to all registered <see cref="T:System.Net.Http.HttpClient" /> with its name as the identifier.
    /// Additional chaos policy handlers with different identifier names can be added using <see cref="M:Microsoft.Extensions.DependencyInjection.HttpResilienceFaultInjectionHttpBuilderExtensions.AddFaultInjectionPolicyHandler(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder,System.String)" />.
    /// </remarks>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services, Action<HttpFaultInjectionOptionsBuilder> configure);
}
