// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class NamedServiceDescriptor<TService>
    where TService : class
{
    public NamedServiceDescriptor(Func<IServiceProvider, TService> implementationFactory, ServiceLifetime lifetime)
    {
        ImplementationFactory = implementationFactory;
        Lifetime = lifetime;
    }

    public ServiceLifetime Lifetime { get; }

    public Func<IServiceProvider, TService> ImplementationFactory { get; }

    public static NamedServiceDescriptor<TService> Describe(Func<IServiceProvider, TService> implementationFactory, ServiceLifetime lifetime)
    {
        return new NamedServiceDescriptor<TService>(implementationFactory, lifetime);
    }
}
