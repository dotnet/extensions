// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for application metadata.
/// </summary>
public static class ApplicationMetadataServiceCollectionExtensions
{
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
