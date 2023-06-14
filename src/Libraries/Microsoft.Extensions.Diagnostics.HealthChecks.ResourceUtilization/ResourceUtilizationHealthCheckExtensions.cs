// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

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

        _ = builder.Services.AddValidatedOptions<ResourceUtilizationHealthCheckOptions, ResourceUtilizationHealthCheckOptionsValidator>();
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

        _ = builder.Services.AddValidatedOptions<ResourceUtilizationHealthCheckOptions, ResourceUtilizationHealthCheckOptionsValidator>();
        return builder.AddCheck<ResourceUtilizationHealthCheck>(HealthCheckName, tags: tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration for <see cref="ResourceUtilizationHealthCheckOptions"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> or <paramref name="section"/> are <see langword="null" />.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ResourceUtilizationHealthCheckOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        IConfigurationSection section)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);

        _ = builder.Services.Configure<ResourceUtilizationHealthCheckOptions>(section);
        return AddResourceUtilizationHealthCheck(builder);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration section holding an instance of <see cref="ResourceUtilizationHealthCheckOptions"/>.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" />, <paramref name="section"/> or <paramref name="tags"/> are <see langword="null" />.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ResourceUtilizationHealthCheckOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        IConfigurationSection section,
        params string[] tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(tags);

        _ = builder.Services.Configure<ResourceUtilizationHealthCheckOptions>(section);
        return AddResourceUtilizationHealthCheck(builder, tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="section">Configuration section holding an instance of <see cref="ResourceUtilizationHealthCheckOptions"/>.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" />, <paramref name="section"/> or <paramref name="tags"/> are <see langword="null" />.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ResourceUtilizationHealthCheckOptions))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        IConfigurationSection section,
        IEnumerable<string> tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(section);
        _ = Throw.IfNull(tags);

        _ = builder.Services.Configure<ResourceUtilizationHealthCheckOptions>(section);
        return AddResourceUtilizationHealthCheck(builder, tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" /> or <paramref name="configure"/> are <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        Action<ResourceUtilizationHealthCheckOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        _ = builder.Services.Configure(configure);
        return AddResourceUtilizationHealthCheck(builder);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" />, <paramref name="configure"/> or <paramref name="tags"/> are <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        Action<ResourceUtilizationHealthCheckOptions> configure,
        params string[] tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);
        _ = Throw.IfNull(tags);

        _ = builder.Services.Configure(configure);
        return AddResourceUtilizationHealthCheck(builder, tags);
    }

    /// <summary>
    /// Registers a health check provider that monitors resource utilization to assess the application's health.
    /// </summary>
    /// <param name="builder">The builder to add the provider to.</param>
    /// <param name="configure">Configuration callback.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder" />, <paramref name="configure"/> or <paramref name="tags"/> are <see langword="null" />.</exception>
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(
        this IHealthChecksBuilder builder,
        Action<ResourceUtilizationHealthCheckOptions> configure,
        IEnumerable<string> tags)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);
        _ = Throw.IfNull(tags);

        _ = builder.Services.Configure(configure);
        return AddResourceUtilizationHealthCheck(builder, tags);
    }
}
