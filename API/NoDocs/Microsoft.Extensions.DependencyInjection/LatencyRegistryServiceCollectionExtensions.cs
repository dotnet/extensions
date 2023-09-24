// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.Extensions.DependencyInjection;

public static class LatencyRegistryServiceCollectionExtensions
{
    public static IServiceCollection RegisterCheckpointNames(this IServiceCollection services, params string[] names);
    public static IServiceCollection RegisterMeasureNames(this IServiceCollection services, params string[] names);
    public static IServiceCollection RegisterTagNames(this IServiceCollection services, params string[] names);
}
