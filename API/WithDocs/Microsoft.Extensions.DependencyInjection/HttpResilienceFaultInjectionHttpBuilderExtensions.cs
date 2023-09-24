// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Resilience.FaultInjection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for Fault-Injection library specifically for HttpClient usages.
/// </summary>
public static class HttpResilienceFaultInjectionHttpBuilderExtensions
{
    /// <summary>
    /// Adds a chaos policy handler identified by the chaos policy options group name to the given <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="chaosPolicyOptionsGroupName">The chaos policy options group name.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    public static IHttpClientBuilder AddFaultInjectionPolicyHandler(this IHttpClientBuilder builder, string chaosPolicyOptionsGroupName);

    /// <summary>
    /// Adds a chaos policy handler to the given <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />
    /// using weight assignments denoted in <paramref name="weightAssignmentsConfig" /> to determine which chaos policy options group to
    /// use at each run of fault-injection.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="weightAssignmentsConfig">Function to configure <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultPolicyWeightAssignmentsOptions" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder builder, Action<FaultPolicyWeightAssignmentsOptions> weightAssignmentsConfig);

    /// <summary>
    /// Adds a chaos policy handler to the given <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />
    /// using weight assignments denoted in <paramref name="weightAssignmentsConfigSection" /> to determine which chaos policy options group to
    /// use at each run of fault-injection.
    /// </summary>
    /// <param name="builder">The <see cref="T:Microsoft.Extensions.DependencyInjection.IHttpClientBuilder" />.</param>
    /// <param name="weightAssignmentsConfigSection">The configuration section to bind to <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultPolicyWeightAssignmentsOptions" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IHttpClientBuilder AddWeightedFaultInjectionPolicyHandlers(this IHttpClientBuilder builder, IConfigurationSection weightAssignmentsConfigSection);
}
