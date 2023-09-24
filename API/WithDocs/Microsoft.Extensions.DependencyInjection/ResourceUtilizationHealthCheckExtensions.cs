// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization'

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Controls resource utilization health check features.
/// </summary>
public static class ResourceUtilizationHealthCheckExtensions
{
    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, params string[] tags);

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IEnumerable<string> tags);

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration for <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilizationHealthCheckOptions" />.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IConfigurationSection section);

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration section holding an instance of <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilizationHealthCheckOptions" />.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" />, <paramref name="section" /> or <paramref name="tags" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IConfigurationSection section, params string[] tags);

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration section holding an instance of <see cref="T:Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilizationHealthCheckOptions" />.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" />, <paramref name="section" /> or <paramref name="tags" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IConfigurationSection section, IEnumerable<string> tags);

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, Action<ResourceUtilizationHealthCheckOptions> configure);

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" />, <paramref name="configure" /> or <paramref name="tags" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, Action<ResourceUtilizationHealthCheckOptions> configure, params string[] tags);

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" />, <paramref name="configure" /> or <paramref name="tags" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, Action<ResourceUtilizationHealthCheckOptions> configure, IEnumerable<string> tags);
}
