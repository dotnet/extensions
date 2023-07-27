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

public class AcceptanceTest
{
    [Fact]
    public async Task CanAddAndActivateSingletonAsync()
    {
        var instanceCount = new InstanceCreatingCounter();
        Assert.Equal(0, instanceCount.Counter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(instanceCount)
                .AddActivatedSingleton<IFakeService, FakeService>())
            .StartAsync();

        var service = host.Services.GetService<IFakeService>();
        await host.StopAsync();

        Assert.NotNull(service);
        Assert.Equal(1, instanceCount.Counter);
    }

    [Fact]
    public async Task SouldIgnoreComponent_WhenNoAutoStartAsync()
    {
        var instanceCount = new InstanceCreatingCounter();
        Assert.Equal(0, instanceCount.Counter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(instanceCount)
                .AddSingleton<IFakeService, FakeService>())
            .StartAsync();

        Assert.Equal(0, instanceCount.Counter);

        var service = host.Services.GetService<IFakeService>();
        await host.StopAsync();

        Assert.NotNull(service);
        Assert.Equal(1, instanceCount.Counter);
    }

    [Fact]
    public async Task ShouldAddAndActivateOnlyOnce_WhenHasChildAsync()
    {
        var parentCount = new InstanceCreatingCounter();
        var childCount = new InstanceCreatingCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services.AddSingleton<IFakeServiceCounter>(childCount)
                .AddSingleton<IFactoryServiceCounter>(parentCount)
                .AddActivatedSingleton(typeof(IFakeService), typeof(FakeService))
                .AddActivatedSingleton<IFactoryService, FactoryService>(_ =>
                {
                    return new FactoryService(_.GetService<IFakeService>()!, _.GetService<IFactoryServiceCounter>()!);
                }))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, childCount.Counter);
        Assert.Equal(1, parentCount.Counter);
    }

    [Fact]
    public async Task ShouldResolveComponentsAutomaticallyAsync()
    {
        var parentCount = new InstanceCreatingCounter();
        var childCount = new InstanceCreatingCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(childCount)
                .AddSingleton<IFactoryServiceCounter>(parentCount)
                .AddSingleton<IFakeService, FakeService>()
                .AddActivatedSingleton<IFactoryService, FactoryService>())
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, childCount.Counter);
        Assert.Equal(1, parentCount.Counter);
    }

    [Fact]
    public async Task CanActivateEnumerableAsync()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddActivatedSingleton(typeof(IFakeService), typeof(FakeService))
                .AddActivatedSingleton(typeof(IFakeService), typeof(FakeOneMultipleService))
                .AddActivatedSingleton(typeof(IFakeService), typeof(AnotherFakeService)))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);

        await host.StopAsync();
    }

    [Fact]
    public async Task CanActivateEnumerableAsync_WithTypeArg()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddActivatedSingleton<IFakeService, FakeService>()
                .AddActivatedSingleton<IFakeService, FakeOneMultipleService>()
                .AddActivatedSingleton<IFakeService, AnotherFakeService>())
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);

        await host.StopAsync();
    }

    [Fact]
    public async Task CanActivateOneServiceAsync()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddSingleton<IFakeService, FakeService>()
                .AddActivatedSingleton<IFakeService, AnotherFakeService>())
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(0, fakeServiceCount.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldActivateService_WhenTypeIsSpecifiedInTypeParameterTService()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(counter)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddActivatedSingleton<FakeService>()
                .AddActivatedSingleton(_ => new AnotherFakeService(_.GetService<IAnotherFakeServiceCounter>()!)))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, counter.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldActivateService_WhenTypeIsSpecifiedInParameter()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(counter)
                .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                .AddActivatedSingleton(typeof(FakeService))
                .AddActivatedSingleton(typeof(AnotherFakeService), _ => new AnotherFakeService(_.GetService<IAnotherFakeServiceCounter>()!)))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, counter.Counter);
        Assert.Equal(1, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task TestStopHostAsync()
    {
        var counter = new InstanceCreatingCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(counter)
                .AddActivatedSingleton<IFakeService, FakeService>())
            .StartAsync();

        Assert.Equal(1, counter.Counter);
        await host.StopAsync();
    }

    [Fact]
    public async Task ShouldNotActivate_WhenServiceOfTypeSpecifiedInTypeParameter_WasAlreadyAdded()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<IFakeServiceCounter>(counter)
                    .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                    .AddSingleton<FakeService>()
                    .AddSingleton<AnotherFakeService>();
                services.TryAddActivatedSingleton(typeof(FakeService));
                services.TryAddActivatedSingleton(typeof(AnotherFakeService), _ => new AnotherFakeService(_.GetService<IAnotherFakeServiceCounter>()!));
            })
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(0, counter.Counter);
        Assert.Equal(0, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldNotActivate_WhenServiceOfTypeSpecifiedInParameter_WasAlreadyAdded()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<IFakeServiceCounter>(counter)
                    .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount)
                    .AddSingleton<FakeService>()
                    .AddSingleton<AnotherFakeService>();
                services.TryAddActivatedSingleton<FakeService>();
                services.TryAddActivatedSingleton(_ => new AnotherFakeService(_.GetService<IAnotherFakeServiceCounter>()!));
            })
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(0, counter.Counter);
        Assert.Equal(0, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldActivateOneSingleton_WhenTryAddIsCalled_WithTypeSpecifiedImplementation()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services =>
                {
                    services
                        .AddSingleton<IFakeServiceCounter>(counter)
                        .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount);
                    services.TryAddActivatedSingleton(typeof(IFakeService), typeof(FakeService));
                    services.TryAddActivatedSingleton(typeof(IFakeService), typeof(AnotherFakeService));
                })
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, counter.Counter);
        Assert.Equal(0, anotherFakeServiceCount.Counter);
    }

    [Fact]
    public async Task ShouldActivateOneSingleton_WhenTryAddIsCalled_WithTypeSpecifiedImplementation_WithTypeArg()
    {
        var counter = new InstanceCreatingCounter();
        var anotherFakeServiceCount = new AnotherFakeServiceCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton<IFakeServiceCounter>(counter)
                    .AddSingleton<IAnotherFakeServiceCounter>(anotherFakeServiceCount);
                services.TryAddActivatedSingleton<IFakeService, FakeService>();
                services.TryAddActivatedSingleton<IFakeService, AnotherFakeService>();
            })
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, counter.Counter);
        Assert.Equal(0, anotherFakeServiceCount.Counter);
    }

    // ------------------------------------------------------------------------------
    [Fact]
    public async Task CanActivateSingletonAsync()
    {
        var instanceCount = new InstanceCreatingCounter();
        Assert.Equal(0, instanceCount.Counter);

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(instanceCount)
                .AddSingleton<IFakeService, FakeService>()
                .Activate<IFakeService>())
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, instanceCount.Counter);

        var service = host.Services.GetService<IFakeService>();

        Assert.NotNull(service);
        Assert.Equal(1, instanceCount.Counter);
    }

    [Fact]
    public async Task ActivationOfNotRegisteredType_ThrowsExceptionAsync()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services.Activate<IFakeService>())
            .Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());

        Assert.Contains(typeof(IFakeService).FullName!, exception.Message);
    }

    [Fact]
    public async Task CanActivateEnumerableImplicitlyAddedAsync()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddSingleton<IFakeService, FakeService>().Activate(typeof(IFakeService))
                .AddSingleton<IFakeService, FakeOneMultipleService>().Activate(typeof(IFakeService)))
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
    }

    [Fact]
    public async Task CanActivateEnumerableImplicitlyAddedAsync_WithTypeArg()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddSingleton<IFakeService, FakeService>().Activate<IFakeService>()
                .AddSingleton<IFakeService, FakeOneMultipleService>().Activate<IFakeService>())
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
    }

    [Fact]
    public async Task CanActivateEnumerableExplicitlyAddedAsync()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeFactoryCount = new InstanceCreatingCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeMultipleCounter>(fakeFactoryCount)
                .AddSingleton<IFakeService, FakeService>()
                .AddSingleton<IFakeService, FakeOneMultipleService>()
                .Activate<IEnumerable<IFakeService>>())
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeFactoryCount.Counter);
    }

    [Fact]
    public async Task CanAutoActivateOpenGenericsAsEnumerableAsync()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeOpenGenericCount = new InstanceCreatingCounter();

        using var host = await new HostBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeOpenGenericCounter>(fakeOpenGenericCount)
                .AddTransient<PocoClass, PocoClass>()
                .AddSingleton(typeof(IFakeOpenGenericService<PocoClass>), typeof(FakeService))
                .AddSingleton(typeof(IFakeOpenGenericService<>), typeof(FakeOpenGenericService<>))
                .Activate<IEnumerable<IFakeOpenGenericService<PocoClass>>>()
                .Activate<IFakeOpenGenericService<DifferentPocoClass>>())
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(2, fakeOpenGenericCount.Counter);
    }

    [Fact]
    public async Task CanAutoActivateClosedGenericsAsEnumerableAsync()
    {
        var fakeServiceCount = new InstanceCreatingCounter();
        var fakeOpenGenericCount = new InstanceCreatingCounter();

        using var host = await FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddSingleton<IFakeServiceCounter>(fakeServiceCount)
                .AddSingleton<IFakeOpenGenericCounter>(fakeOpenGenericCount)
                .AddTransient<PocoClass, PocoClass>()
                .AddSingleton(typeof(IFakeOpenGenericService<PocoClass>), typeof(FakeService))
                .AddSingleton<IFakeOpenGenericService<PocoClass>, FakeOpenGenericService<PocoClass>>()
                .Activate<IEnumerable<IFakeOpenGenericService<PocoClass>>>())
            .StartAsync();
        await host.StopAsync();

        Assert.Equal(1, fakeServiceCount.Counter);
        Assert.Equal(1, fakeOpenGenericCount.Counter);
    }
}
