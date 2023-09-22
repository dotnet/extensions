// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.FaultInjection;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Provides extension methods for Fault-Injection library specifically for HttpClient usages.
/// </summary>
public static class HttpClientFaultInjectionExtensions
{
    /// <summary>
    /// Registers default implementations for <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IFaultInjectionOptionsProvider" />, <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IChaosPolicyFactory" /> and <see cref="T:Microsoft.Extensions.Http.Resilience.FaultInjection.IHttpClientChaosPolicyFactory" />;
    /// adds fault-injection policies to all <see cref="T:System.Net.Http.HttpClient" />.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <returns>
    /// The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
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
    /// <returns>
    /// The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
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
    /// <returns>
    /// The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// If the default instance of <see cref="T:System.Net.Http.IHttpClientFactory" /> is used, this method also adds a
    /// chaos policy handler to all registered <see cref="T:System.Net.Http.HttpClient" /> with its name as the identifier.
    /// Additional chaos policy handlers with different identifier names can be added using <see cref="M:Microsoft.Extensions.Http.Resilience.FaultInjection.HttpClientFaultInjectionExtensions.AddFaultInjectionPolicyHandler(Microsoft.Extensions.DependencyInjection.IHttpClientBuilder,System.String)" />.
    /// </remarks>
    public static IServiceCollection AddHttpClientFaultInjection(this IServiceCollection services, Action<HttpFaultInjectionOptionsBuilder> configure);

    /// <summary>
    /// Adds a chaos policy handler identified by the chaos policy options group name to the given <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.
    /// </summary>
    /// <param name="httpClientBuilder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="chaosPolicyOptionsGroupName">The chaos policy options group name.</param>
    /// <returns>
    /// The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static IHttpClientBuilder AddFaultInjectionPolicyHandler(this IHttpClientBuilder httpClientBuilder, string chaosPolicyOptionsGroupName);

    /// <summary>
    /// Adds a chaos policy handler to the given <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />
    /// using weight assignments denoted in <paramref name="weightAssignmentsConfig" /> to determine which chaos policy options group to
    /// use at each run of fault-injection.
    /// </summary>
    /// <param name="httpClientBuilder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="weightAssignmentsConfig">Function to configure <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultPolicyWeightAssignmentsOptions" />.</param>
    /// <returns>
    /// The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" /> so that additional calls can be chained.
    /// </returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder httpClientBuilder, Action<FaultPolicyWeightAssignmentsOptions> weightAssignmentsConfig);

    /// <summary>
    /// Adds a chaos policy handler to the given <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />
    /// using weight assignments denoted in <paramref name="weightAssignmentsConfigSection" /> to determine which chaos policy options group to
    /// use at each run of fault-injection.
    /// </summary>
    /// <param name="httpClientBuilder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="weightAssignmentsConfigSection">The configuration section to bind to <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultPolicyWeightAssignmentsOptions" />.</param>
    /// <returns>
    /// The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" /> so that additional calls can be chained.
    /// </returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder httpClientBuilder, IConfigurationSection weightAssignmentsConfigSection);
}
