// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Test.Fakes;
using Microsoft.Extensions.DependencyInjection.Test.Helpers;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Test;

public class AutoActivationExtensionsKeyedTests
{
    [Fact]
    public void AddActivatedKeyedSingleton_Throws_WhenArgumentsAreNull()
    {
        var serviceCollection = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton<IFakeService, FakeService>(null!, null, (_, _) => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton<IFakeService, FakeService>(serviceCollection, null, null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton<FakeService>(null!, null, (_, _) => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton<FakeService>(serviceCollection, null, null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton<FakeService>(null!, null));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton(null!, typeof(FakeService), null));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton(serviceCollection, null!, null));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton<IFakeService, FakeService>(null!, null));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton(null!, typeof(FakeService), null, (_, _) => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton(serviceCollection, null!, null, (_, _) => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton(serviceCollection, typeof(FakeService), null, implementationFactory: null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton(null!, typeof(IFakeService), null, typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton(serviceCollection, null!, null, typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedKeyedSingleton(serviceCollection, typeof(IFakeService), null, implementationType: null!));
    }

    [Fact]
    public void TryAddActivatedKeyedSingleton_Throws_WhenArgumentsAreNull()
    {
        var serviceCollection = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton(null!, typeof(FakeService), null));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton(serviceCollection, null!, null));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton(null!, typeof(IFakeService), null, typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton(serviceCollection, null!, null, typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton(serviceCollection, typeof(IFakeService), null, implementationType: null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton(null!, typeof(FakeService), null, (_, _) => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton(serviceCollection, null!, null, (_, _) => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton(serviceCollection, typeof(FakeService), null, implementationFactory: null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton<FakeService>(null!, null));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton<IFakeService, FakeService>(null!, null));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton<FakeService>(null!, null, (_, _) => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedKeyedSingleton<FakeService>(serviceCollection, null, null!));
    }

    [Fact]
    public void AutoActivate_Adds_OneHostedService()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<IFakeServiceCounter>(new InstanceCreatingCounter());
        serviceCollection.AddActivatedKeyedSingleton<IFakeService, FakeService>(null);
        Assert.Equal(1, serviceCollection.Count(d => d.ImplementationType == typeof(AutoActivationHostedService)));

        serviceCollection.AddActivatedKeyedSingleton<IFactoryService, FactoryService>(null);
        Assert.Equal(1, serviceCollection.Count(d => d.ImplementationType == typeof(AutoActivationHostedService)));
    }
}
