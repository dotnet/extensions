// Assembly 'Microsoft.Extensions.Diagnostics.Probes'

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Probes;

public static class KubernetesProbesExtensions
{
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services);
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddKubernetesProbes(this IServiceCollection services, Action<KubernetesProbesOptions> configure);
}
