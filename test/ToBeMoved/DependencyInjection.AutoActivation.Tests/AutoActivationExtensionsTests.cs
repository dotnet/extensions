// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Test.Fakes;
using Microsoft.Extensions.DependencyInjection.Test.Helpers;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Test;

public class AutoActivationExtensionsTests
{
    [Fact]
    public void Activate_Throws_WhenArgumentsAreNull()
    {
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.Activate<IFakeService>(null!));
    }

    [Fact]
    public void AddActivatedSingleton_Throws_WhenArgumentsAreNull()
    {
        var serviceCollection = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton<IFakeService, FakeService>(null!, _ => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton<IFakeService, FakeService>(serviceCollection, null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton<FakeService>(null!, _ => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton<FakeService>(serviceCollection, null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton<FakeService>(null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton(null!, typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton(serviceCollection, null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton<IFakeService, FakeService>(null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton(null!, typeof(FakeService), _ => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton(serviceCollection, null!, _ => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton(serviceCollection, typeof(FakeService), implementationFactory: null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton(null!, typeof(IFakeService), typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton(serviceCollection, null!, typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.AddActivatedSingleton(serviceCollection, typeof(IFakeService), implementationType: null!));
    }

    [Fact]
    public void TryAddActivatedSingleton_Throws_WhenArgumentsAreNull()
    {
        var serviceCollection = new ServiceCollection();
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton(null!, typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton(serviceCollection, null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton(null!, typeof(IFakeService), typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton(serviceCollection, null!, typeof(FakeService)));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton(serviceCollection, typeof(IFakeService), implementationType: null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton(null!, typeof(FakeService), _ => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton(serviceCollection, null!, _ => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton(serviceCollection, typeof(FakeService), implementationFactory: null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton<FakeService>(null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton<IFakeService, FakeService>(null!));

        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton<FakeService>(null!, _ => null!));
        Assert.Throws<ArgumentNullException>(() => AutoActivationExtensions.TryAddActivatedSingleton<FakeService>(serviceCollection, null!));
    }

    [Fact]
    public void AutoActivate_Adds_OneHostedService()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton<IFakeServiceCounter>(new InstanceCreatingCounter());
        serviceCollection.AddActivatedSingleton<IFakeService, FakeService>();
        Assert.Equal(1, serviceCollection.Count(d => d.ImplementationType == typeof(AutoActivationHostedService)));

        serviceCollection.AddActivatedSingleton<IFactoryService, FactoryService>();
        Assert.Equal(1, serviceCollection.Count(d => d.ImplementationType == typeof(AutoActivationHostedService)));
    }
}
