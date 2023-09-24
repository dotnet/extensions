// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using Microsoft.AspNetCore.HeaderParsing;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for header parsing.
/// </summary>
public static class HeaderParsingServiceCollectionExtensions
{
    /// <summary>
    /// Adds the header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services);

    /// <summary>
    /// Adds the header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="configuration">A delegate to setup parsing for the header.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, Action<HeaderParsingOptions> configuration);

    /// <summary>
    /// Adds the header parsing feature.
    /// </summary>
    /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the services to.</param>
    /// <param name="section">A configuration section.</param>
    /// <returns>The value of <paramref name="services" />.</returns>
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, IConfigurationSection section);
}
