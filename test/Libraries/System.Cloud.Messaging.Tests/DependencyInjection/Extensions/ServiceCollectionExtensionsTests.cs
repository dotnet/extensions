// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Cloud.Messaging.DependencyInjection.Internal;
using System.Cloud.Messaging.DependencyInjection.Tests.Data;
using System.Cloud.Messaging.DependencyInjection.Tests.Data.Consumers;
using System.Cloud.Messaging.DependencyInjection.Tests.Data.Delegates;
using System.Cloud.Messaging.DependencyInjection.Tests.Data.Middlewares;
using System.Cloud.Messaging.DependencyInjection.Tests.Data.Sources;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace System.Cloud.Messaging.DependencyInjection.Tests.Extensions;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("pipeline-1", "abc", false)]
    public async Task AddNamedMessageProcessingPipeline_ShouldWorkCorrectly_WhenMessageSourceKeepsProducingMessage(string pipelineName, string message, bool mocksVerificationEnabled)
    {
        var context = new TestMessageContext(new FeatureCollection(), Encoding.UTF8.GetBytes(message));

        Mock<IMessageSource> mockSource = new();
        mockSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>())).Returns(new ValueTask<MessageContext>(context));

        var mocks = new TestMocks();
        IHostBuilder hostBuilder = FakeHost.CreateBuilder(TestMocks.GetFakeHostOptions());
        hostBuilder.ConfigureServices(services =>
        {
            // Create a message consumer pipeline.
            services.AddAsyncPipeline(pipelineName)
                    .ConfigureMessageSource(_ => mockSource.Object)
                    .AddMessageMiddleware(_ => new SampleMiddleware(mocks.MockDelegate.Object))
                    .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(mocks.MockMessageDestination.Object).InvokeAsync)
                    .ConfigureMessageConsumer(sp => new DerivedConsumer(sp.GetMessageSource(pipelineName),
                                                                        sp.GetMessageMiddlewares(pipelineName),
                                                                        sp.GetMessageDelegate(pipelineName),
                                                                        sp.GetRequiredService<ILogger>()))
                    .RunConsumerAsBackgroundService();
        });

        // Build the host.
        using var host = hostBuilder.Build();

        // Validate the hosted services include ConsumerBackgroundService.
        var hostedServices = host.Services.GetServices<IHostedService>().ToList();
        int consumerServicesCount = hostedServices.Count(x => x is ConsumerBackgroundService);
        Assert.Equal(1, consumerServicesCount);

        // Start and stop the host.
        await host.StartAsync();
        await host.StopAsync();

        // Verify Mocks
        if (mocksVerificationEnabled)
        {
            int minInteractions = 1;
            mockSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.AtLeast(minInteractions));
            mocks.VerifyMocksHavingCountAtLeast(minInteractions);
        }
    }

    [Theory]
    [InlineData(1, "pipeline-1", "m1,m2,m3", 3)]
    [InlineData(2, "pipeline-2", "n1,n2,n3", 3)]
    public async Task AddNamedMessageProcessingPipeline_ShouldWorkCorrectly(int numberOfBackgroundServices, string pipelineName, string messages, int countOfMessages)
    {
        var mocks = new TestMocks();
        IHostBuilder hostBuilder = FakeHost.CreateBuilder(TestMocks.GetFakeHostOptions());
        hostBuilder.ConfigureServices(services =>
        {
            services.TryAddSingleton(_ => new SampleSource(TestMocks.GetMessages(messages)));

            // Create a message consumer pipeline builder.
            var builder = services.AddAsyncPipeline(pipelineName)
                                  .ConfigureMessageSource<SampleSource>()
                                  .AddMessageMiddleware(_ => new SampleMiddleware(mocks.MockDelegate.Object))
                                  .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(mocks.MockMessageDestination.Object).InvokeAsync)
                                  .ConfigureMessageConsumer(sp => new SampleConsumer(sp.GetMessageSource(pipelineName),
                                                                                     sp.GetMessageMiddlewares(pipelineName),
                                                                                     sp.GetMessageDelegate(pipelineName),
                                                                                     sp.GetRequiredService<ILogger>()));

            // Run multiple background services for message consumer.
            for (int i = 0; i < numberOfBackgroundServices; i++)
            {
                builder.RunConsumerAsBackgroundService();
            }
        });

        // Build the host.
        using var host = hostBuilder.Build();

        // Validate the hosted services include ConsumerBackgroundServices.
        var hostedServices = host.Services.GetServices<IHostedService>().ToList();
        int consumerServicesCount = hostedServices.Count(x => x is ConsumerBackgroundService);
        Assert.Equal(numberOfBackgroundServices, consumerServicesCount);

        // Start and stop the host.
        await host.StartAsync();
        await host.StopAsync();

        // Verify Mocks
        if (numberOfBackgroundServices == 1)
        {
            mocks.VerifyMocksHavingCount(countOfMessages);
        }
        else
        {
            mocks.VerifyMocksHavingCountAtLeast(countOfMessages);
        }
    }

    [Theory]
    [InlineData("pipeline-m-", 1, "m1,m2,m3", 3)]
    [InlineData("pipeline-n-", 2, "n1,n2,n3", 6)]
    public async Task AddMultipleNamedMessageProcessingPipeline_ShouldWorkCorrectly(string pipelineNamePrefix, int pipelineCount, string messages, int countOfMessages)
    {
        var mocks = new TestMocks();
        IHostBuilder hostBuilder = FakeHost.CreateBuilder(TestMocks.GetFakeHostOptions());
        hostBuilder.ConfigureServices(services =>
        {
            // Create a message consumer pipeline.
            for (int i = 1; i <= pipelineCount; i++)
            {
                string pipelineName = pipelineNamePrefix + i;
                services.AddAsyncPipeline(pipelineName)
                        .ConfigureMessageSource(_ => new SampleSource(TestMocks.GetMessages(messages)))
                        .AddMessageMiddleware(_ => new SampleMiddleware(mocks.MockDelegate.Object))
                        .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(mocks.MockMessageDestination.Object).InvokeAsync)
                        .ConfigureMessageConsumer(sp => new SampleConsumer(sp.GetMessageSource(pipelineName),
                                                                           sp.GetMessageMiddlewares(pipelineName),
                                                                           sp.GetMessageDelegate(pipelineName),
                                                                           sp.GetRequiredService<ILogger>()))
                        .RunConsumerAsBackgroundService();
            }
        });

        // Build the host.
        using var host = hostBuilder.Build();

        // Validate the hosted services include ConsumerBackgroundServices.
        var hostedServices = host.Services.GetServices<IHostedService>().ToList();
        int consumerServicesCount = hostedServices.Count(x => x is ConsumerBackgroundService);
        Assert.Equal(pipelineCount, consumerServicesCount);

        // Start and stop the host.
        await host.StartAsync();
        await host.StopAsync();

        // Verify Mocks
        mocks.VerifyMocksHavingCount(countOfMessages);
    }

    private class TestMocks
    {
        public static FakeHostOptions GetFakeHostOptions() => new()
        {
            StartUpTimeout = TimeSpan.FromMinutes(4),
            ShutDownTimeout = TimeSpan.FromMinutes(4),
            TimeToLive = TimeSpan.FromMinutes(10),
            FakeLogging = true,
        };

        public Mock<IMessageDestination> MockMessageDestination = new();
        public Mock<MessageDelegate> MockDelegate = new();

        public TestMocks()
        {
            // Setup Mocks
            MockMessageDestination.Setup(x => x.WriteAsync(It.IsAny<MessageContext>())).Returns(new ValueTask(Task.CompletedTask));
            MockDelegate.Setup(x => x.Invoke(It.IsAny<MessageContext>())).Returns(new ValueTask(Task.CompletedTask));
        }

        public static string[] GetMessages(string csv)
        {
            return csv.Split(',');
        }

        public void VerifyMocksHavingCount(int count)
        {
            MockDelegate.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Exactly(count));
            MockMessageDestination.Verify(x => x.WriteAsync(It.IsAny<MessageContext>()), Times.Exactly(count));
        }

        public void VerifyMocksHavingCountAtLeast(int count)
        {
            MockDelegate.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.AtLeast(count));
            MockMessageDestination.Verify(x => x.WriteAsync(It.IsAny<MessageContext>()), Times.AtLeast(count));
        }
    }
}
