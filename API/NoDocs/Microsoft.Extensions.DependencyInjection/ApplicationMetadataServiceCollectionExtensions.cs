// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AmbientMetadata;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class ApplicationMetadataServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, Action<ApplicationMetadata> configure);
}
