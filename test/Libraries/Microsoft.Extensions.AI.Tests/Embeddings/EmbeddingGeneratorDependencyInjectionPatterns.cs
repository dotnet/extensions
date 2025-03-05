// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class EmbeddingGeneratorDependencyInjectionPatterns
{
    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddEmbeddingGenerator_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        var builder = lifetime.HasValue
            ? sc.AddEmbeddingGenerator(services => new TestEmbeddingGenerator(), lifetime.Value)
            : sc.AddEmbeddingGenerator(services => new TestEmbeddingGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IEmbeddingGenerator<string, Embedding<float>>), sd.ServiceType);
        Assert.False(sd.IsKeyedService);
        Assert.Null(sd.ImplementationInstance);
        Assert.NotNull(sd.ImplementationFactory);
        Assert.IsType<TestEmbeddingGenerator>(sd.ImplementationFactory(null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void AddKeyedEmbeddingGenerator_RegistersExpectedLifetime(ServiceLifetime? lifetime)
    {
        ServiceCollection sc = new();
        ServiceLifetime expectedLifetime = lifetime ?? ServiceLifetime.Singleton;
        var builder = lifetime.HasValue
            ? sc.AddKeyedEmbeddingGenerator("key", services => new TestEmbeddingGenerator(), lifetime.Value)
            : sc.AddKeyedEmbeddingGenerator("key", services => new TestEmbeddingGenerator());

        ServiceDescriptor sd = Assert.Single(sc);
        Assert.Equal(typeof(IEmbeddingGenerator<string, Embedding<float>>), sd.ServiceType);
        Assert.True(sd.IsKeyedService);
        Assert.Equal("key", sd.ServiceKey);
        Assert.Null(sd.KeyedImplementationInstance);
        Assert.NotNull(sd.KeyedImplementationFactory);
        Assert.IsType<TestEmbeddingGenerator>(sd.KeyedImplementationFactory(null!, null!));
        Assert.Equal(expectedLifetime, sd.Lifetime);
    }
}
