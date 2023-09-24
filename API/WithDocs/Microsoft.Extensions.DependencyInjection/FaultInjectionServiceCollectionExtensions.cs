// Assembly 'Microsoft.Extensions.Resilience'

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Resilience.FaultInjection;
using Polly;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for Fault-Injection library.
/// </summary>
public static class FaultInjectionServiceCollectionExtensions
{
    /// <summary>
    /// Registers default implementations for <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IFaultInjectionOptionsProvider" /> and <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IChaosPolicyFactory" />.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddFaultInjection(this IServiceCollection services);

    /// <summary>
    /// Configures <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" /> and registers default implementations for
    /// <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IFaultInjectionOptionsProvider" /> and <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IChaosPolicyFactory" />.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="section">
    /// The configuration section to bind to <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" />.
    /// </param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddFaultInjection(this IServiceCollection services, IConfiguration section);

    /// <summary>
    /// Calls the given action to configure <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" /> and registers default implementations for
    /// <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IFaultInjectionOptionsProvider" /> and <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IChaosPolicyFactory" />.
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="configure">Function to configure <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.FaultInjectionOptions" />.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any parameter is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddFaultInjection(this IServiceCollection services, Action<FaultInjectionOptionsBuilder> configure);

    /// <summary>
    /// Associates the given <see cref="T:Polly.Context" /> instance to the given identifier name
    /// for an <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" /> registered at <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.IFaultInjectionOptionsProvider" />.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <param name="groupName">The identifier name for an <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" />.</param>
    /// <returns>The value of <paramref name="context" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any argument is <see langword="null" />.
    /// </exception>
    public static Context WithFaultInjection(this Context context, string groupName);

    /// <summary>
    /// Associates <paramref name="weightAssignments" /> to the calling <see cref="T:Polly.Context" /> instance, where <paramref name="weightAssignments" />
    /// will be used to determine which <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" /> to use at each fault-injection run.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <param name="weightAssignments">The fault policy weight assignment.</param>
    /// <returns>The value of <paramref name="context" />.</returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any argument is <see langword="null" />.
    /// </exception>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static Context WithFaultInjection(this Context context, FaultPolicyWeightAssignmentsOptions weightAssignments);

    /// <summary>
    /// Gets the name of the registered <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" /> from <see cref="T:Polly.Context" />.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <returns>
    /// The <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ChaosPolicyOptionsGroup" /> if registered; <see langword="null" /> if it isn't.
    /// </returns>
    /// <exception cref="T:System.ArgumentNullException">
    /// Any argument is <see langword="null" />.
    /// </exception>
    public static string? GetFaultInjectionGroupName(this Context context);
}
