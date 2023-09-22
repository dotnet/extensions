// Assembly 'Microsoft.Extensions.ObjectPool.DependencyInjection'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.ObjectPool;

[Experimental("EXTEXP0010", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class ObjectPoolServiceCollectionExtensions
{
    public static IServiceCollection AddPooled<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this IServiceCollection services, Action<DependencyInjectionPoolOptions>? configure = null) where TService : class;
    public static IServiceCollection AddPooled<TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this IServiceCollection services, Action<DependencyInjectionPoolOptions>? configure = null) where TService : class where TImplementation : class, TService;
    public static IServiceCollection ConfigurePool<TService>(this IServiceCollection services, Action<DependencyInjectionPoolOptions> configure) where TService : class;
    public static IServiceCollection ConfigurePools(this IServiceCollection services, IConfigurationSection section);
}
