// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001
#pragma warning disable S3604

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileClientBuilderTests
{
    [Fact]
    public void InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new HostedFileClientBuilder((IHostedFileClient)null!));
        Assert.Throws<ArgumentNullException>("innerClientFactory", () => new HostedFileClientBuilder((Func<IServiceProvider, IHostedFileClient>)null!));

        using var innerClient = new TestHostedFileClient();
        var builder = new HostedFileClientBuilder(innerClient);
        Assert.Throws<ArgumentNullException>("clientFactory", () => builder.Use((Func<IHostedFileClient, IHostedFileClient>)null!));
        Assert.Throws<ArgumentNullException>("clientFactory", () => builder.Use((Func<IHostedFileClient, IServiceProvider, IHostedFileClient>)null!));
    }

    [Fact]
    public void PassesServiceProviderToFactories()
    {
        var expectedServiceProvider = new ServiceCollection().BuildServiceProvider();
        using var expectedInnerClient = new TestHostedFileClient();
        using var expectedOuterClient = new TestHostedFileClient();

        var builder = new HostedFileClientBuilder(services =>
        {
            Assert.Same(expectedServiceProvider, services);
            return expectedInnerClient;
        });

        builder.Use((innerClient, serviceProvider) =>
        {
            Assert.Same(expectedServiceProvider, serviceProvider);
            Assert.Same(expectedInnerClient, innerClient);
            return expectedOuterClient;
        });

        Assert.Same(expectedOuterClient, builder.Build(expectedServiceProvider));
    }

    [Fact]
    public void BuildsPipelineInOrderAdded()
    {
        using var expectedInnerClient = new TestHostedFileClient();
        var builder = new HostedFileClientBuilder(expectedInnerClient);

        builder.Use(next => new InnerClientCapturingHostedFileClient("First", next));
        builder.Use(next => new InnerClientCapturingHostedFileClient("Second", next));
        builder.Use(next => new InnerClientCapturingHostedFileClient("Third", next));

        var first = (InnerClientCapturingHostedFileClient)builder.Build();

        Assert.Equal("First", first.Name);
        var second = (InnerClientCapturingHostedFileClient)first.InnerClient;
        Assert.Equal("Second", second.Name);
        var third = (InnerClientCapturingHostedFileClient)second.InnerClient;
        Assert.Equal("Third", third.Name);
        Assert.Same(expectedInnerClient, third.InnerClient);
    }

    [Fact]
    public void DoesNotAllowFactoriesToReturnNull()
    {
        using var innerClient = new TestHostedFileClient();
        HostedFileClientBuilder builder = new(innerClient);
        builder.Use(_ => null!);
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void UsesEmptyServiceProviderWhenNoServicesProvided()
    {
        using var innerClient = new TestHostedFileClient();
        HostedFileClientBuilder builder = new(innerClient);
        builder.Use((inner, serviceProvider) =>
        {
            Assert.Null(serviceProvider.GetService(typeof(object)));

            var keyedServiceProvider = Assert.IsAssignableFrom<IKeyedServiceProvider>(serviceProvider);
            Assert.Null(keyedServiceProvider.GetKeyedService(typeof(object), "key"));
            Assert.Throws<InvalidOperationException>(() => keyedServiceProvider.GetRequiredKeyedService(typeof(object), "key"));

            return inner;
        });
        builder.Build();
    }

    private sealed class InnerClientCapturingHostedFileClient(string name, IHostedFileClient innerClient)
        : DelegatingHostedFileClient(innerClient)
    {
        public string Name { get; } = name;
        public new IHostedFileClient InnerClient => base.InnerClient;
    }
}
