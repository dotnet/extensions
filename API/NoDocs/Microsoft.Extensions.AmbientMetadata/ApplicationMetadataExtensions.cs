// Assembly 'Microsoft.Extensions.AmbientMetadata.Application'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.AmbientMetadata;

public static class ApplicationMetadataExtensions
{
    public static IHostBuilder UseApplicationMetadata(this IHostBuilder builder, string sectionName = "ambientmetadata:application");
    public static IConfigurationBuilder AddApplicationMetadata(this IConfigurationBuilder builder, IHostEnvironment hostEnvironment, string sectionName = "ambientmetadata:application");
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddApplicationMetadata(this IServiceCollection services, Action<ApplicationMetadata> configure);
}
