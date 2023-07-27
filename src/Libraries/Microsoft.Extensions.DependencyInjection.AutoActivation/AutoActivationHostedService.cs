// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class AutoActivationHostedService : IHostedService
{
    private readonly AutoActivatorOptions _options;
    private readonly IServiceProvider _provider;

    public AutoActivationHostedService(IServiceProvider provider, IOptions<AutoActivatorOptions> options)
    {
        _provider = provider;
        _options = Throw.IfMemberNull(options, options.Value);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var singleton in _options.AutoActivators)
        {
            _ = _provider.GetRequiredService(singleton);
        }

        foreach (var (serviceType, serviceKey) in _options.KeyedAutoActivators)
        {
            _ = _provider.GetRequiredKeyedService(serviceType, serviceKey);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
