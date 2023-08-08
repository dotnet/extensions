// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.DependencyInjection.Tests.Data.Delegates;
using System.Cloud.Messaging.DependencyInjection.Tests.Data.Middlewares;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace System.Cloud.Messaging.DependencyInjection.Tests.Internal;

/// <summary>
/// Tests for <see cref="IPipelineDelegateFactory"/>.
/// </summary>
public class PipelineDelegateFactoryTests
{
    [Fact]
    public void PipelineBuild_ShouldThrowException_WhenTerminalDelegateIsNotConfigured()
    {
        IHostBuilder hostBuilder = new HostBuilder();
        hostBuilder.ConfigureServices(services => { });
        using var host = hostBuilder.Build();

        var serviceProvider = host.Services;
        var exception = Assert.Throws<InvalidOperationException>(serviceProvider.GetRequiredService<MessageDelegate>);
    }

    [Theory]
    [InlineData("pipeline-1")]
    public void PipelineBuild_ShouldWorkCorrectly_WhenTerminalDelegateIsConfigured(string pipelineName)
    {
        IHostBuilder hostBuilder = new HostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddAsyncPipeline(pipelineName)
                    .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(new Mock<IMessageDestination>().Object).InvokeAsync)
                    .ConfigureMessageConsumer(_ => new Mock<MessageConsumer>().Object);
        });

        using var host = hostBuilder.Build();
        var serviceProvider = host.Services;

        var messageDelegate = serviceProvider.GetKeyedService<MessageDelegate>(pipelineName);
        Assert.NotNull(messageDelegate);
    }

    [Theory]
    [InlineData("pipeline-2")]
    public void PipelineBuild_ShouldWorkCorrectly_WhenMiddlewareAndTerminalDelegateIsConfigured(string pipelineName)
    {
        IHostBuilder hostBuilder = new HostBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            services.AddAsyncPipeline(pipelineName)
                    .AddMessageMiddleware(_ => new SampleMiddleware(new Mock<MessageDelegate>().Object))
                    .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(new Mock<IMessageDestination>().Object).InvokeAsync)
                    .ConfigureMessageConsumer(_ => new Mock<MessageConsumer>().Object);
        });

        using var host = hostBuilder.Build();
        var serviceProvider = host.Services;

        var messageDelegate = serviceProvider.GetKeyedService<MessageDelegate>(pipelineName);
        Assert.NotNull(messageDelegate);
    }
}
