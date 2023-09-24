// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Configuration;

public static class ApplicationMetadataConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddApplicationMetadata(this IConfigurationBuilder builder, IHostEnvironment hostEnvironment, string sectionName = "ambientmetadata:application");
}
