// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class NamedServiceProvider<TService> : INamedServiceProvider<TService>
    where TService : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<NamedServiceProviderOptions<TService>> _optionsMonitor;
    private readonly ConcurrentDictionary<NamedServiceDescriptor<TService>, Lazy<TService>> _cache = new();

    private readonly Func<NamedServiceDescriptor<TService>, Lazy<TService>> _factory;

    public NamedServiceProvider(IServiceProvider serviceProvider,
        IOptionsMonitor<NamedServiceProviderOptions<TService>> optionsMonitor)
    {
        _serviceProvider = serviceProvider;
        _optionsMonitor = optionsMonitor;
        _factory = CreateTService;
    }

    public TService? GetService(string name)
    {
        var options = _optionsMonitor.Get(name);
        int count = options.Services.Count;
        if (count == 0)
        {
            return null;
        }

        // the last one wins
        var serviceDescriptor = options.Services[count - 1];
        return GetOrCreateTService(serviceDescriptor);
    }

    public IEnumerable<TService> GetServices(string name)
    {
        var options = _optionsMonitor.Get(name);
        int count = options.Services.Count;
        if (count == 0)
        {
            return Enumerable.Empty<TService>();
        }

        var collection = new List<TService>(count);
        foreach (var serviceDescriptor in options.Services)
        {
            collection.Add(GetOrCreateTService(serviceDescriptor));
        }

        return collection;
    }

    private TService GetOrCreateTService(NamedServiceDescriptor<TService> serviceDescriptor)
    {
        if (_cache.TryGetValue(serviceDescriptor, out var lazy))
        {
            return lazy.Value;
        }

        if (serviceDescriptor.Lifetime == ServiceLifetime.Transient)
        {
            return serviceDescriptor.ImplementationFactory(_serviceProvider);
        }

        return _cache.GetOrAdd(serviceDescriptor, _factory).Value;
    }

    private Lazy<TService> CreateTService(NamedServiceDescriptor<TService> serviceDescriptor)
    {
        return new Lazy<TService>(
            () => serviceDescriptor.ImplementationFactory(_serviceProvider),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
