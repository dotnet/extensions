// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.DocumentExtraction;

public class DocumentExtractionClientBuilderTests
{
    [Fact]
    public void PassingNullInnerClientThrows()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new DocumentExtractionClientBuilder((IDocumentExtractionClient)null!));
        Assert.Throws<ArgumentNullException>("innerClientFactory", () => new DocumentExtractionClientBuilder((Func<IServiceProvider, IDocumentExtractionClient>)null!));
    }

    [Fact]
    public void BuildReturnsInnerClientWhenNoMiddleware()
    {
        using var inner = new TestDocumentExtractionClient();
        var builder = inner.AsBuilder();

        var built = builder.Build();

        Assert.Same(inner, built);
    }

    [Fact]
    public void UseAppliesFactoriesInReverseOrderSoFirstAddedIsOutermost()
    {
        // Arrange
        using var inner = new TestDocumentExtractionClient();
        var order = new List<string>();

        var built = inner.AsBuilder()
            .Use(c =>
            {
                order.Add("outer-built");
                return new InspectorDocumentExtractionClient(c, "outer");
            })
            .Use(c =>
            {
                order.Add("inner-built");
                return new InspectorDocumentExtractionClient(c, "inner");
            })
            .Build();

        // The first factory added should be the outermost wrapper.
        var outer = Assert.IsType<InspectorDocumentExtractionClient>(built);
        Assert.Equal("outer", outer.Name);
        var innerWrapper = Assert.IsType<InspectorDocumentExtractionClient>(outer.InnerClientPublic);
        Assert.Equal("inner", innerWrapper.Name);
        Assert.Same(inner, innerWrapper.InnerClientPublic);

        // Reverse-order application: inner factory runs before outer factory.
        Assert.Equal(["inner-built", "outer-built"], order);
    }

    [Fact]
    public void BuildThrowsWhenFactoryReturnsNull()
    {
        using var inner = new TestDocumentExtractionClient();
        var builder = inner.AsBuilder().Use(_ => null!);

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void UseNullFactoryThrows()
    {
        using var inner = new TestDocumentExtractionClient();
        var builder = inner.AsBuilder();
        Assert.Throws<ArgumentNullException>("clientFactory", () => builder.Use((Func<IDocumentExtractionClient, IDocumentExtractionClient>)null!));
        Assert.Throws<ArgumentNullException>("clientFactory", () => builder.Use((Func<IDocumentExtractionClient, IServiceProvider, IDocumentExtractionClient>)null!));
    }

    [Fact]
    public void ServicesAreFlowedThroughBuild()
    {
        using var inner = new TestDocumentExtractionClient();
        IServiceProvider? observed = null;

        var services = new ServiceCollection().BuildServiceProvider();
        _ = inner.AsBuilder()
            .Use((c, sp) =>
            {
                observed = sp;
                return c;
            })
            .Build(services);

        Assert.Same(services, observed);
    }

    private sealed class InspectorDocumentExtractionClient(IDocumentExtractionClient inner, string name) : DelegatingDocumentExtractionClient(inner)
    {
        public string Name => name;
        public IDocumentExtractionClient InnerClientPublic => InnerClient;
    }
}
