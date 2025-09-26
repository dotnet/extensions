// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ImageGeneratorBuilderTests
{
    [Fact]
    public void PassesServiceProviderToFactories()
    {
        var expectedServiceProvider = new ServiceCollection().BuildServiceProvider();
        using TestImageGenerator expectedInnerGenerator = new();
        using TestImageGenerator expectedOuterGenerator = new();

        var builder = new ImageGeneratorBuilder(services =>
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
        // Arrange
        using TestImageGenerator expectedInnerGenerator = new();
        var builder = new ImageGeneratorBuilder(expectedInnerGenerator);

        builder.Use(next => new InnerGeneratorCapturingImageGenerator("First", next));
        builder.Use(next => new InnerGeneratorCapturingImageGenerator("Second", next));
        builder.Use(next => new InnerGeneratorCapturingImageGenerator("Third", next));

        // Act
        var first = (InnerGeneratorCapturingImageGenerator)builder.Build();

        // Assert
        Assert.Equal("First", first.Name);
        var second = (InnerGeneratorCapturingImageGenerator)first.InnerGenerator;
        Assert.Equal("Second", second.Name);
        var third = (InnerGeneratorCapturingImageGenerator)second.InnerGenerator;
        Assert.Equal("Third", third.Name);
        Assert.Same(expectedInnerGenerator, third.InnerGenerator);
    }

    [Fact]
    public void DoesNotAcceptNullInnerService()
    {
        Assert.Throws<ArgumentNullException>("innerGenerator", () => new ImageGeneratorBuilder((IImageGenerator)null!));
        Assert.Throws<ArgumentNullException>("innerGenerator", () => ((IImageGenerator)null!).AsBuilder());
    }

    [Fact]
    public void DoesNotAcceptNullFactories()
    {
        Assert.Throws<ArgumentNullException>("innerGeneratorFactory", () => new ImageGeneratorBuilder((Func<IServiceProvider, IImageGenerator>)null!));
    }

    [Fact]
    public void DoesNotAllowFactoriesToReturnNull()
    {
        using var innerGenerator = new TestImageGenerator();
        ImageGeneratorBuilder builder = new(innerGenerator);
        builder.Use(_ => null!);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("entry at index 0", ex.Message);
    }

    [Fact]
    public void UsesEmptyServiceProviderWhenNoServicesProvided()
    {
        using var innerGenerator = new TestImageGenerator();
        ImageGeneratorBuilder builder = new(innerGenerator);
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

    private sealed class InnerGeneratorCapturingImageGenerator(string name, IImageGenerator innerGenerator) : DelegatingImageGenerator(innerGenerator)
    {
#pragma warning disable S3604 // False positive: Member initializer values should not be redundant
        public string Name { get; } = name;
#pragma warning restore S3604
        public new IImageGenerator InnerGenerator => base.InnerGenerator;
    }
}
