// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Test.Fakes;
using Microsoft.Extensions.DependencyInjection.Test.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Test;

public partial class AcceptanceTest
{
    [Fact]
    public async Task CanAddAndActivateKeyedSingletonAsync()
    {
        var instanceCount = new InstanceCreatingCounter();
        Assert.Equal(0, instanceCount.Counter);

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(instanceCount)
                .AddActivatedKeyedSingleton<IFakeService, FakeService>(serviceKey))
            .StartAsync();

        Assert.Equal(1, instanceCount.Counter);

        var service = host.Services.GetKeyedService<IFakeService>(serviceKey);
        await host.StopAsync();

        Assert.NotNull(service);
        Assert.Equal(1, instanceCount.Counter);
    }

    [Fact]
    public async Task ShouldAddAndActivateOnlyOnce_WhenHasChildAsync_Keyed()
    {
        var parentCount = new InstanceCreatingCounter();
        var childCount = new InstanceCreatingCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services.AddSingleton<IFakeServiceCounter>(childCount)
                .AddSingleton<IFactoryServiceCounter>(parentCount)
                .AddActivatedKeyedSingleton(typeof(IFakeService), serviceKey, typeof(FakeService))
                .AddActivatedKeyedSingleton<IFactoryService, FactoryService>(serviceKey, (sp, sk) =>
                {
                    return new FactoryService(sp.GetKeyedService<IFakeService>(sk)!, sp.GetRequiredService<IFactoryServiceCounter>());
                }))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, childCount.Counter);
        Assert.Equal(1, parentCount.Counter);
    }

    [Fact]
    public async Task ShouldResolveComponentsAutomaticallyAsync_Keyed()
    {
        var parentCount = new InstanceCreatingCounter();
        var childCount = new InstanceCreatingCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(childCount)
                .AddSingleton<IFactoryServiceCounter>(parentCount)
                .AddSingleton<IFakeService, FakeService>()
                .AddActivatedKeyedSingleton<IFactoryService, FactoryService>(serviceKey))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, childCount.Counter);
        Assert.Equal(1, parentCount.Counter);
    }

    [Fact]
    public async Task CanActivateEnumerableAsync_Keyed()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddActivatedKeyedSingleton(typeof(IFakeService), serviceKey, typeof(FakeService))
                .AddActivatedKeyedSingleton(typeof(IFakeService), serviceKey, typeof(FakeOneMultipleService))
                .AddActivatedKeyedSingleton(typeof(IFakeService), serviceKey, typeof(AnotherFakeService)))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);

        await host.StopAsync();
    }

    [Fact]
    public async Task CanActivateEnumerableAsync_WithTypeArg_Keyed()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddActivatedKeyedSingleton<IFakeService, FakeService>(serviceKey)
                .AddActivatedKeyedSingleton<IFakeService, FakeOneMultipleService>(serviceKey)
                .AddActivatedKeyedSingleton<IFakeService, AnotherFakeService>(serviceKey))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);

        await host.StopAsync();
    }

    [Fact]
    public async Task CanActivateOneServiceAsync_Keyed()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddSingleton<IFakeService, FakeService>()
                .AddActivatedKeyedSingleton<IFakeService, AnotherFakeService>(serviceKey))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(0, fakeServiceCount.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldActivateService_WhenTypeIsSpecifiedInTypeParameterTService_Keyed()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(counter)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddActivatedKeyedSingleton<FakeService>(serviceKey)
                .AddActivatedKeyedSingleton(serviceKey, (sp, _) => new AnotherFakeService(sp.GetRequiredService<IAnotherFakeServiceCounter>())))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, counter.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldActivateService_WhenTypeIsSpecifiedInParameter_Keyed()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(counter)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddActivatedKeyedSingleton(typeof(FakeService), serviceKey)
                .AddActivatedKeyedSingleton(typeof(AnotherFakeService), serviceKey, (sp, _) => new AnotherFakeService(sp.GetService<IAnotherFakeServiceCounter>()!)))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, counter.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task TestStopHostAsync_Keyed()
    {
        var counter = new InstanceCreatingCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(counter)
                .AddActivatedKeyedSingleton<IFakeService, FakeService>(serviceKey))
            .StartAsync();

        Assert.Equal(1, counter.Counter);
        await host.StopAsync();
    }

    [Fact]
    public async Task ShouldNotActivate_WhenServiceOfTypeSpecifiedInTypeParameter_WasAlreadyAdded_Keyed()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<IFakeServiceCounter>(counter)
                    .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                    .AddKeyedSingleton<FakeService>(serviceKey)
                    .AddKeyedSingleton<AnotherFakeService>(serviceKey);
                services.TryAddActivatedKeyedSingleton(typeof(FakeService), serviceKey);
                services.TryAddActivatedKeyedSingleton(typeof(AnotherFakeService), serviceKey, (sp, _) => new AnotherFakeService(sp.GetService<IAnotherFakeServiceCounter>()!));
            })
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(0, counter.Counter);
        Assert.Equal(0, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldNotActivate_WhenServiceOfTypeSpecifiedInParameter_WasAlreadyAdded_Keyed()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<IFakeServiceCounter>(counter)
                    .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                    .AddKeyedSingleton<FakeService>(serviceKey)
                    .AddKeyedSingleton<AnotherFakeService>(serviceKey);
                services.TryAddActivatedKeyedSingleton<FakeService>(serviceKey);
                services.TryAddActivatedKeyedSingleton(serviceKey, (sp, _) => new AnotherFakeService(sp.GetService<IAnotherFakeServiceCounter>()!));
            })
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(0, counter.Counter);
        Assert.Equal(0, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldActivateOneSingleton_WhenTryAddIsCalled_WithTypeSpecifiedImplementation_Keyed()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services =>
                {
                    services
                        .AddSingleton<IFakeServiceCounter>(counter)
                        .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount);
                    services.TryAddActivatedKeyedSingleton(typeof(IFakeService), serviceKey, typeof(FakeService));
                    services.TryAddActivatedKeyedSingleton(typeof(IFakeService), serviceKey, typeof(AnotherFakeService));
                })
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, counter.Counter);
        Assert.Equal(0, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldActivateOneSingleton_WhenTryAddIsCalled_WithTypeSpecifiedImplementation_WithTypeArg_Keyed()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<IFakeServiceCounter>(counter)
                    .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount);
                services.TryAddActivatedKeyedSingleton<IFakeService, FakeService>(serviceKey);
                services.TryAddActivatedKeyedSingleton<IFakeService, AnotherFakeService>(serviceKey);
            })
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, counter.Counter);
        Assert.Equal(0, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task CanActivateSingletonAsync_Keyed()
    {
        var instanceCount = new InstanceCreatingCounter();
        Assert.Equal(0, instanceCount.Counter);

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(instanceCount)
                .AddKeyedSingleton<IFakeService, FakeService>(serviceKey)
                .ActivateKeyedSingleton<IFakeService>(serviceKey))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, instanceCount.Counter);

        var service = host.Services.GetKeyedService<IFakeService>(serviceKey);

        Assert.NotNull(service);
        Assert.Equal(1, instanceCount.Counter);
    }

    [Fact]
    public async Task ActivationOfNotRegisteredType_ThrowsExceptionAsync_Keyed()
    {
        var serviceKey = new object();
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services.ActivateKeyedSingleton<IFakeService>(serviceKey))
            .Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains(typeof(IFakeService).FullName!, exception.Message);
    }

    [Fact]
    public async Task CanActivateEnumerableImplicitlyAddedAsync_Keyed()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddKeyedSingleton<IFakeService, FakeService>(serviceKey).ActivateKeyedSingleton<IFakeService>(serviceKey)
                .AddKeyedSingleton<IFakeService, FakeOneMultipleService>(serviceKey).ActivateKeyedSingleton<IFakeService>(serviceKey))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
    }

    [Fact]
    public async Task CanActivateEnumerableExplicitlyAddedAsync_Keyed()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddKeyedSingleton<IFakeService, FakeService>(serviceKey)
                .AddKeyedSingleton<IFakeService, FakeOneMultipleService>(serviceKey)
                .ActivateKeyedSingleton<IEnumerable<IFakeService>>(serviceKey))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
    }

    [Fact]
    public async Task CanAutoActivateOpenGenericsAsEnumerableAsync_Keyed()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeOpenGenericCount = new InstanceCreatingCounter();

        var serviceKey = new object();
        using var host = await new HostBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeOpenGenericCounter>(fakeOpenGenericCount)
                .AddTransient<PocoClass, PocoClass>()
                .AddKeyedSingleton(typeof(IFakeOpenGenericService<PocoClass>), serviceKey, typeof(FakeService))
                .AddKeyedSingleton(typeof(IFakeOpenGenericService<>), serviceKey, typeof(FakeOpenGenericService<>))
                .ActivateKeyedSingleton<IEnumerable<IFakeOpenGenericService<PocoClass>>>(serviceKey)
                .ActivateKeyedSingleton<IFakeOpenGenericService<DifferentPocoClass>>(serviceKey))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(2, fakeOpenGenericCount.Counter);
    }

    [Fact]
    public async Task CanAutoActivateClosedGenericsAsEnumerableAsync_Keyed()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeOpenGenericCount = new InstanceCreatingCounter();

        var serviceKey = new object();
        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeOpenGenericCounter>(fakeOpenGenericCount)
                .AddTransient<PocoClass, PocoClass>()
                .AddKeyedSingleton(typeof(IFakeOpenGenericService<PocoClass>), serviceKey, typeof(FakeService))
                .AddKeyedSingleton<IFakeOpenGenericService<PocoClass>, FakeOpenGenericService<PocoClass>>(serviceKey)
                .ActivateKeyedSingleton<IEnumerable<IFakeOpenGenericService<PocoClass>>>(serviceKey))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeOpenGenericCount.Counter);
    }
}
