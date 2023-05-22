// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class AutoActivationHostedService : IHostedService
{
    private readonly Type[] _autoActivators;
    private readonly IServiceProvider _provider;

    public AutoActivationHostedService(IServiceProvider provider, IOptions<AutoActivatorOptions> options)
    {
        _provider = provider;
        var value = Throw.IfMemberNull(options, options.Value);

        _autoActivators = value.AutoActivators.ToArray();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var singleton in _autoActivators)
        {
            _ = _provider.GetRequiredService(singleton);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
