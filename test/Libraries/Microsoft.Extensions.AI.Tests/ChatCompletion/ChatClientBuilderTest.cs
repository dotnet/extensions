// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ChatClientBuilderTest
{
    [Fact]
    public void PassesServiceProviderToFactories()
    {
        var expectedServiceProvider = new ServiceCollection().BuildServiceProvider();
        using TestChatClient expectedInnerClient = new();
        using TestChatClient expectedOuterClient = new();

        var builder = new ChatClientBuilder(services =>
        {
            Assert.Same(expectedServiceProvider, services);
            return expectedInnerClient;
        });

        builder.Use((serviceProvider, innerClient) =>
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
        // Arrange
        using TestChatClient expectedInnerClient = new();
        var builder = new ChatClientBuilder(expectedInnerClient);

        builder.Use(next => new InnerClientCapturingChatClient("First", next));
        builder.Use(next => new InnerClientCapturingChatClient("Second", next));
        builder.Use(next => new InnerClientCapturingChatClient("Third", next));

        // Act
        var first = (InnerClientCapturingChatClient)builder.Build();

        // Assert
        Assert.Equal("First", first.Name);
        var second = (InnerClientCapturingChatClient)first.InnerClient;
        Assert.Equal("Second", second.Name);
        var third = (InnerClientCapturingChatClient)second.InnerClient;
        Assert.Equal("Third", third.Name);
        Assert.Same(expectedInnerClient, third.InnerClient);
    }

    [Fact]
    public void DoesNotAcceptNullInnerService()
    {
        Assert.Throws<ArgumentNullException>("innerClient", () => new ChatClientBuilder((IChatClient)null!));
        Assert.Throws<ArgumentNullException>("innerClient", () => ((IChatClient)null!).AsBuilder());
    }

    [Fact]
    public void DoesNotAcceptNullFactories()
    {
        Assert.Throws<ArgumentNullException>("innerClientFactory", () => new ChatClientBuilder((Func<IServiceProvider, IChatClient>)null!));
    }

    [Fact]
    public void DoesNotAllowFactoriesToReturnNull()
    {
        using var innerClient = new TestChatClient();
        ChatClientBuilder builder = new(innerClient);
        builder.Use(_ => null!);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("entry at index 0", ex.Message);
    }

    private sealed class InnerClientCapturingChatClient(string name, IChatClient innerClient) : DelegatingChatClient(innerClient)
    {
#pragma warning disable S3604 // False positive: Member initializer values should not be redundant
        public string Name { get; } = name;
#pragma warning restore S3604
        public new IChatClient InnerClient => base.InnerClient;
    }
}
