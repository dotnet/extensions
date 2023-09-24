// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Extensions for application metadata.
/// </summary>
public static class ApplicationMetadataConfigurationBuilderExtensions
{
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
}
