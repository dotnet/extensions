// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Controls resource utilization health check features.
/// </summary>
public static class ResourceUtilizationHealthCheckExtensions
{
    private const string HealthCheckName = "container resources";

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, params string[] tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(tags);

        _ = builder.Services.AddResourceMonitoring();

        _ = builder.Services.AddOptionsWithValidateOnStart<ResourceUtilizationHealthCheckOptions, ResourceUtilizationHealthCheckOptionsValidator>();
        return builder.AddCheck<ResourceUtilizationHealthCheck>(HealthCheckName, tags: tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IEnumerable<string> tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(tags);

        _ = builder.Services.AddResourceMonitoring();

        _ = builder.Services.AddOptionsWithValidateOnStart<ResourceUtilizationHealthCheckOptions, ResourceUtilizationHealthCheckOptionsValidator>();
        return builder.AddCheck<ResourceUtilizationHealthCheck>(HealthCheckName, tags: tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration for <see cref="ResourceUtilizationHealthCheckOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> or <paramref name="section"/> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services.Configure<ResourceUtilizationHealthCheckOptions>(section);
        return builder.AddResourceUtilizationHealthCheck();
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration section holding an instance of <see cref="ResourceUtilizationHealthCheckOptions"/>.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" />, <paramref name="section"/> or <paramref name="tags"/> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        IConfigurationSection section,
        params string[] tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(tags);

        _ = builder.Services.Configure<ResourceUtilizationHealthCheckOptions>(section);
        return builder.AddResourceUtilizationHealthCheck(tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration section holding an instance of <see cref="ResourceUtilizationHealthCheckOptions"/>.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" />, <paramref name="section"/> or <paramref name="tags"/> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        IConfigurationSection section,
        IEnumerable<string> tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(tags);

        _ = builder.Services.Configure<ResourceUtilizationHealthCheckOptions>(section);
        return builder.AddResourceUtilizationHealthCheck(tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> or <paramref name="configure"/> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        Action<ResourceUtilizationHealthCheckOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.Configure(configure);
        return builder.AddResourceUtilizationHealthCheck();
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" />, <paramref name="configure"/> or <paramref name="tags"/> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        Action<ResourceUtilizationHealthCheckOptions> configure,
        params string[] tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);
        _ = Throw.IfNull(tags);

        _ = builder.Services.Configure(configure);
        return builder.AddResourceUtilizationHealthCheck(tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" />, <paramref name="configure"/> or <paramref name="tags"/> is <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        Action<ResourceUtilizationHealthCheckOptions> configure,
        IEnumerable<string> tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);
        _ = Throw.IfNull(tags);

        _ = builder.Services.Configure(configure);
        return builder.AddResourceUtilizationHealthCheck(tags);
    }
}
