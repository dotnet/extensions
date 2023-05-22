// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.NamedService.Test;

public class ResolutionTests : IDisposable
{
    private const string NamedSingleton = nameof(NamedSingleton);
    private const string NamedTransient = nameof(NamedTransient);
    private readonly ServiceProvider _provider;

    public ResolutionTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddNamedSingleton<TestClass, DuplicateTestClass>(NamedSingleton);
        serviceCollection.AddNamedSingleton<TestClass>(NamedSingleton);
        serviceCollection.AddNamedTransient<TestClass>(NamedTransient);
        _provider = serviceCollection.BuildServiceProvider();
    }

    [Fact]
    public void GetRequiredService_WhenTypeIsRegistered_ShouldReturnNewObject()
    {
        var namedProvider = _provider.GetRequiredService<INamedServiceProvider<TestClass>>();
        var service = namedProvider.GetRequiredService(NamedSingleton);
        service.Should().BeOfType<TestClass>();
    }

    [Fact]
    public void GetRequiredService_WhenResolveSingletonSecondTime_ShouldReturnSameObject()
    {
        var namedProvider = _provider.GetRequiredService<INamedServiceProvider<TestClass>>();
        var service = namedProvider.GetRequiredService(NamedSingleton);
        var service2 = namedProvider.GetRequiredService(NamedSingleton);
        service2.Should().BeOfType<TestClass>();
        service2.Should().BeSameAs(service);
    }

    [Fact]
    public void GetRequiredService_WhenResolveTransientSecondTime_ShouldReturnNewObject()
    {
        var namedProvider = _provider.GetRequiredService<INamedServiceProvider<TestClass>>();
        var service = namedProvider.GetRequiredService(NamedTransient);
        var service2 = namedProvider.GetRequiredService(NamedTransient);
        service2.Should().NotBeSameAs(service);
    }

    [Fact]
    public void GetService_WhenNotRegistered_ShouldReturnNull()
    {
        var namedProvider = _provider.GetRequiredService<INamedServiceProvider<TestClass>>();
        var service = namedProvider.GetService("bogus");
        service.Should().BeNull();
    }

    [Fact]
    public void GetRequiredService_WhenNotRegistered_ShouldThrow()
    {
        var namedProvider = _provider.GetRequiredService<INamedServiceProvider<TestClass>>();
        Assert.Throws<InvalidOperationException>(() => namedProvider.GetRequiredService("bogus"));
    }

    [Fact]
    public void GetServices_WhenMultipleTypes_ReturnsCollection()
    {
        var namedProvider = _provider.GetRequiredService<INamedServiceProvider<TestClass>>();
        var collection = namedProvider.GetServices(NamedSingleton).ToList();
        collection.Should().HaveCount(2);
        collection.First().Should().BeOfType<DuplicateTestClass>();
        collection.Skip(1).First().Should().BeOfType<TestClass>();
    }

    private class TestClass
    {
    }

    private sealed class DuplicateTestClass : TestClass
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _provider.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
