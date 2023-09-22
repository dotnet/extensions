// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.AmbientMetadata;

/// <summary>
/// Extensions for application metadata.
/// </summary>
public static class ApplicationMetadataExtensions
{
    /// <summary>
    /// Registers a configuration provider for application metadata and binds a model object onto the configuration.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="sectionName">Section name to bind configuration from. Default set to "ambientmetadata:application".</param>
    /// <returns>The value of <paramref name="builder" />&gt;.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException"><paramref name="sectionName" /> is either <see langword="null" />, empty or whitespace.</exception>
    public static IHostBuilder UseApplicationMetadata(this IHostBuilder builder, string sectionName = "ambientmetadata:application");

    /// <summary>
    /// Registers a configuration provider for application metadata.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="hostEnvironment">An instance of <see cref="T:Microsoft.Extensions.Hosting.IHostEnvironment" />.</param>
    /// <param name="sectionName">Section name to save configuration into. Default set to "ambientmetadata:application".</param>
    /// <returns>The value of <paramref name="builder" />&gt;.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> or <paramref name="hostEnvironment" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException"><paramref name="sectionName" /> is either <see langword="null" />, empty or whitespace.</exception>
    public static IConfigurationBuilder AddApplicationMetadata(this IConfigurationBuilder builder, IHostEnvironment hostEnvironment, string sectionName = "ambientmetadata:application");

    /// <summary>
    /// Adds an instance of <see cref="T:Microsoft.Extensions.AmbientMetadata.ApplicationMetadata" /> to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the instance to.</param>
    /// <param name="section">The configuration section to bind.</param>
    /// <returns>The value of <paramref name="services" />&gt;.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="section" /> or <paramref name="section" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, IConfigurationSection section);

    /// <summary>
    /// Adds an instance of <see cref="T:Microsoft.Extensions.AmbientMetadata.ApplicationMetadata" /> to a dependency injection container.
    /// </summary>
    /// <param name="services">The dependency injection container to add the instance to.</param>
    /// <param name="configure">The delegate to configure <see cref="T:Microsoft.Extensions.AmbientMetadata.ApplicationMetadata" /> with.</param>
    /// <returns>The value of <paramref name="services" />&gt;.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.</exception>
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, Action<ApplicationMetadata> configure);
}
