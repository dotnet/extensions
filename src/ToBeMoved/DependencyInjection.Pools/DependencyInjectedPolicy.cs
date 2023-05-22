// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection.Pools;

internal sealed class DependencyInjectedPolicy<TDefinition, TImplementation> : IPooledObjectPolicy<TDefinition>
    where TDefinition : class
    where TImplementation : class, TDefinition
{
    private readonly IServiceProvider _provider;
    private readonly ObjectFactory _factory;
    private readonly bool _isResettable;

    public DependencyInjectedPolicy(IServiceProvider provider)
    {
        _provider = provider;
        _factory = ActivatorUtilities.CreateFactory(typeof(TImplementation), Type.EmptyTypes);
        _isResettable = typeof(IResettable).IsAssignableFrom(typeof(TImplementation));
    }

    public TDefinition Create() => (TDefinition)_factory(_provider, Array.Empty<object?>());

    public bool Return(TDefinition obj)
    {
        if (_isResettable)
        {
            return ((IResettable)obj).TryReset();
        }

        return true;
    }
}
