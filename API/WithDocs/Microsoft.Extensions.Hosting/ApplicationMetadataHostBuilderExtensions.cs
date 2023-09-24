// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AmbientMetadata;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extensions for application metadata.
/// </summary>
public static class ApplicationMetadataHostBuilderExtensions
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
}
