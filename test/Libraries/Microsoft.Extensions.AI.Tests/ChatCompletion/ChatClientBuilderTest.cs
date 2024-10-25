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
        using TestChatClient expectedResult = new();
        var builder = new ChatClientBuilder(expectedServiceProvider);

        builder.Use((serviceProvider, innerClient) =>
        {
            Assert.Same(expectedServiceProvider, serviceProvider);
            return expectedResult;
        });

        using TestChatClient innerClient = new();
        Assert.Equal(expectedResult, builder.Use(innerClient: innerClient));
    }

    [Fact]
    public void BuildsPipelineInOrderAdded()
    {
        // Arrange
        using TestChatClient expectedInnerClient = new();
        var builder = new ChatClientBuilder();

        builder.Use(next => new InnerClientCapturingChatClient("First", next));
        builder.Use(next => new InnerClientCapturingChatClient("Second", next));
        builder.Use(next => new InnerClientCapturingChatClient("Third", next));

        // Act
        var first = (InnerClientCapturingChatClient)builder.Use(expectedInnerClient);

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
        Assert.Throws<ArgumentNullException>(() => new ChatClientBuilder().Use((IChatClient)null!));
    }

    [Fact]
    public void DoesNotAcceptNullFactories()
    {
        ChatClientBuilder builder = new();
        Assert.Throws<ArgumentNullException>(() => builder.Use((Func<IChatClient, IChatClient>)null!));
        Assert.Throws<ArgumentNullException>(() => builder.Use((Func<IServiceProvider, IChatClient, IChatClient>)null!));
    }

    [Fact]
    public void DoesNotAllowFactoriesToReturnNull()
    {
        ChatClientBuilder builder = new();
        builder.Use(_ => null!);
        var ex = Assert.Throws<InvalidOperationException>(() => builder.Use(new TestChatClient()));
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
