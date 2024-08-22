// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Shared.DiagnosticIds;

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

#if !NET5_0_OR_GREATER
#pragma warning disable CS0436 // Type conflicts with imported type
#endif
    [Obsolete("This API is obsolete and will be removed in a future version. Consider using observable instruments.",
        DiagnosticId = DiagnosticIds.Obsoletions.ResourceMonitoring,
        UrlFormat = DiagnosticIds.UrlFormat)]
    public IResourceMonitorBuilder AddPublisher<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
        where T : class, IResourceUtilizationPublisher
    {
        Services.TryAddEnumerable(ServiceDescriptor.Singleton<IResourceUtilizationPublisher, T>());

        return this;
    }
}
