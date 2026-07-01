// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class VideoGeneratorBuilderTests
{
    [Fact]
    public void PassesServiceProviderToFactories()
    {
        var expectedServiceProvider = new ServiceCollection().BuildServiceProvider();
        using TestVideoGenerator expectedInnerGenerator = new();
        using TestVideoGenerator expectedOuterGenerator = new();

        var builder = new VideoGeneratorBuilder(services =>
        {
            Assert.Same(expectedServiceProvider, services);
            return expectedInnerGenerator;
        });

        builder.Use((innerGenerator, serviceProvider) =>
        {
            Assert.Same(expectedServiceProvider, serviceProvider);
            Assert.Same(expectedInnerGenerator, innerGenerator);
            return expectedOuterGenerator;
        });

        Assert.Same(expectedOuterGenerator, builder.Build(expectedServiceProvider));
    }

    [Fact]
    public void BuildsPipelineInOrderAdded()
    {
        using TestVideoGenerator expectedInnerGenerator = new();
        var builder = new VideoGeneratorBuilder(expectedInnerGenerator);

        builder.Use(next => new InnerGeneratorCapturingVideoGenerator("First", next));
        builder.Use(next => new InnerGeneratorCapturingVideoGenerator("Second", next));
        builder.Use(next => new InnerGeneratorCapturingVideoGenerator("Third", next));

        var first = (InnerGeneratorCapturingVideoGenerator)builder.Build();

        Assert.Equal("First", first.Name);
        var second = (InnerGeneratorCapturingVideoGenerator)first.InnerGenerator;
        Assert.Equal("Second", second.Name);
        var third = (InnerGeneratorCapturingVideoGenerator)second.InnerGenerator;
        Assert.Equal("Third", third.Name);
        Assert.Same(expectedInnerGenerator, third.InnerGenerator);
    }

    [Fact]
    public void DoesNotAcceptNullInnerService()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new VideoGeneratorBuilder((IVideoGenerator)null!));
        Assert.Throws<ArgumentNullException>("innerGenerator", () => ((IVideoGenerator)null!).AsBuilder());
    }

    [Fact]
    public void DoesNotAcceptNullFactories()
    {
        Assert.Throws<ArgumentNullException>("innerGeneratorFactory", () => new VideoGeneratorBuilder((Func<IServiceProvider, IVideoGenerator>)null!));
    }

    [Fact]
    public void DoesNotAllowFactoriesToReturnNull()
    {
        using var innerGenerator = new TestVideoGenerator();
        VideoGeneratorBuilder builder = new(innerGenerator);
        builder.Use(_ => null!);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("entry at index 0", ex.Message);
    }

    [Fact]
    public void UsesEmptyServiceProviderWhenNoServicesProvided()
    {
        using var innerGenerator = new TestVideoGenerator();
        VideoGeneratorBuilder builder = new(innerGenerator);
        builder.Use((innerGenerator, serviceProvider) =>
        {
            Assert.Null(serviceProvider.GetService(typeof(object)));

            var keyedServiceProvider = Assert.IsAssignableFrom<IKeyedServiceProvider>(serviceProvider);
            Assert.Null(keyedServiceProvider.GetKeyedService(typeof(object), "key"));
            Assert.Throws<InvalidOperationException>(() => keyedServiceProvider.GetRequiredKeyedService(typeof(object), "key"));

            return innerGenerator;
        });
        builder.Build();
    }

    private sealed class InnerGeneratorCapturingVideoGenerator(string name, IVideoGenerator innerGenerator) : DelegatingVideoGenerator(innerGenerator)
    {
#pragma warning disable S3604 // False positive: Member initializer values should not be redundant
        public string Name { get; } = name;
#pragma warning restore S3604
        public new IVideoGenerator InnerGenerator => base.InnerGenerator;
    }
}
