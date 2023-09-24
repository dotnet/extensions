// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Latency;

public static class LatencyRegistryExtensions
{
    public static IServiceCollection RegisterCheckpointNames(this IServiceCollection services, params string[] names);
    public static IServiceCollection RegisterMeasureNames(this IServiceCollection services, params string[] names);
    public static IServiceCollection RegisterTagNames(this IServiceCollection services, params string[] names);
}
