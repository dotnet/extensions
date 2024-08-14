// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal sealed class ResourceMonitorBuilder : IResourceMonitorBuilder
{
    public IServiceCollection Services { get; }

    public ResourceMonitorBuilder(IServiceCollection services)
    {
        services.TryAddSingleton<ResourceMonitorService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ResourceMonitorService>(static sp => sp.GetRequiredService<ResourceMonitorService>()));
        services.TryAddSingleton<IResourceMonitor>(static sp => sp.GetRequiredService<ResourceMonitorService>());

        Services = services;
    }

    [Obsolete("This method is obsolete. Instead, use observable instruments from Microsoft.Extensions.Diagnostics.ResourceMonitoring.ResourceUtilizationInstruments.")]
    public IResourceMonitorBuilder AddPublisher<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class, IResourceUtilizationPublisher
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceUtilizationPublisher, T>());

        return this;
    }
}
