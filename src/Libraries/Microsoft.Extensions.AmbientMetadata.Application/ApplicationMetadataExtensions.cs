// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AmbientMetadata;

/// <summary>
/// Extensions for application metadata.
/// </summary>
public static class ApplicationMetadataExtensions
{
    private const string DefaultSectionName = "ambientmetadata:application";

    /// <summary>
    /// Registers a configuration provider for application metadata and binds a model object onto the configuration.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="sectionName">Section name to bind configuration from. Default set to "ambientmetadata:application".</param>
    /// <returns>The value of <paramref name="builder"/>>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sectionName"/> is either <see langword="null"/>, empty or whitespace.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ApplicationMetadata))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IHostBuilder UseApplicationMetadata(this IHostBuilder builder, string sectionName = DefaultSectionName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrWhitespace(sectionName);

        _ = builder
            .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                configurationBuilder.AddApplicationMetadata(hostBuilderContext.HostingEnvironment, sectionName))
            .ConfigureServices((hostBuilderContext, serviceCollection) =>
                serviceCollection.AddApplicationMetadata(hostBuilderContext.Configuration.GetSection(sectionName)));

        return builder;
    }

    /// <summary>
    /// Registers a configuration provider for application metadata.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment" />.</param>
    /// <param name="sectionName">Section name to save configuration into. Default set to "ambientmetadata:application".</param>
    /// <returns>The value of <paramref name="builder"/>>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="hostEnvironment"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="sectionName"/> is either <see langword="null"/>, empty or whitespace.</exception>
    public static IConfigurationBuilder AddApplicationMetadata(this IConfigurationBuilder builder, IHostEnvironment hostEnvironment, string sectionName = DefaultSectionName)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(hostEnvironment);
        _ = Throw.IfNullOrWhitespace(sectionName);

        return builder.Add(new ApplicationMetadataSource(hostEnvironment, sectionName));
    }

    /// <summary>
    /// Adds an instance of <see cref="ApplicationMetadata"/> to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the instance to.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The value of <paramref name="services"/>>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> or <paramref name="section"/> are <see langword="null"/>.</exception>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(ApplicationMetadata))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicDependency]")]
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, IConfigurationSection section)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(section);

        _ = services.AddValidatedOptions<ApplicationMetadata, ApplicationMetadataValidator>().Bind(section);

        return services;
    }

    /// <summary>
    /// Adds an instance of <see cref="ApplicationMetadata"/> to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the instance to.</param>
    /// <param name="configure">The delegate to configure <see cref="ApplicationMetadata"/> with.</param>
    /// <returns>The value of <paramref name="services"/>>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> are <see langword="null"/>.</exception>
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, Action<ApplicationMetadata> configure)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNull(configure);

        _ = services.AddValidatedOptions<ApplicationMetadata, ApplicationMetadataValidator>().Configure(configure);

        return services;
    }
}
