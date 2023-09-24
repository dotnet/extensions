// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using Microsoft.AspNetCore.HeaderParsing;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class HeaderParsingServiceCollectionExtensions
{
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services);
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, Action<HeaderParsingOptions> configuration);
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, IConfigurationSection section);
}
