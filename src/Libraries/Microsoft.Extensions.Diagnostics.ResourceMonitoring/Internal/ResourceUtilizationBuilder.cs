// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

internal sealed class ResourceUtilizationBuilder : IResourceMonitorBuilder
{
    public IServiceCollection Services { get; }

    public ResourceUtilizationBuilder(IServiceCollection services)
    {
        services.TryAddSingleton<ResourceUtilizationTrackerService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ResourceUtilizationTrackerService>(static sp => sp.GetRequiredService<ResourceUtilizationTrackerService>()));
        services.TryAddSingleton<IResourceMonitor>(static sp => sp.GetRequiredService<ResourceUtilizationTrackerService>());

        Services = services;
    }

    public IResourceMonitorBuilder AddPublisher<T>()
        where T : class, IResourceUtilizationPublisher
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceUtilizationPublisher, T>());

        return this;
    }
}
