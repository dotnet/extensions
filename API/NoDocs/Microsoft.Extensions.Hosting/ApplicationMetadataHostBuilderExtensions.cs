// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AmbientMetadata;

namespace Microsoft.Extensions.Hosting;

public static class ApplicationMetadataHostBuilderExtensions
{
    public static IHostBuilder UseApplicationMetadata(this IHostBuilder builder, string sectionName = "ambientmetadata:application");
}
