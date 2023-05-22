// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Cloud.Messaging.Tests.Data.Consumers;
using System.Cloud.Messaging.Tests.Data.Delegates;
using System.Cloud.Messaging.Tests.Data.Middlewares;
using System.Cloud.Messaging.Tests.Data.Sources;
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
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Xunit;

namespace System.Cloud.Messaging.Tests.Extensions.Startup;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("pipeline-1", "abc", false)]
    public async Task AddNamedMessageProcessingPipeline_ShouldWorkCorrectly_WhenMessageSourceKeepsProducingMessage(string pipelineName, string message, bool mocksVerificationEnabled)
    {
        var context = new MessageContext(new FeatureCollection());
        context.SetMessageSourceFeatures(new FeatureCollection());
        context.SetSourcePayload(Encoding.UTF8.GetBytes(message));

        Mock<IMessageSource> mockSource = new();
        mockSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>())).Returns(new ValueTask<MessageContext>(context));

        var mocks = new TestMocks();
        IHostBuilder hostBuilder = FakeHost.CreateBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            mocks.RegisterCommonServices(services);

            // Create a message consumer pipeline.
            services.AddAsyncPipeline(pipelineName)
                    .ConfigureMessageSource(_ => mockSource.Object)
                    .AddMessageMiddleware(_ => new SampleMiddleware(mocks.MockDelegate.Object))
                    .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(mocks.MockMessageDestination.Object))
                    .ConfigureMessageConsumer(sp =>
                    {
                        var messageSource = sp.GetRequiredService<INamedServiceProvider<IMessageSource>>().GetRequiredService(pipelineName);
                        var messageDelegate = sp.GetRequiredService<INamedServiceProvider<IMessageDelegate>>().GetRequiredService(pipelineName);
                        var logger = sp.GetRequiredService<ILogger>();
                        return new DerivedConsumer(messageSource, messageDelegate, logger);
                    })
                    .RunConsumerAsBackgroundService();
        });

        // Build the host.
        using IHost host = hostBuilder.Build();

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
            mocks.VerifyMocksHavingCountAtleast(minInteractions);
        }
    }

    [Theory]
    [InlineData(1, "pipeline-1", "abc,pqr,xyz", 3)]
    [InlineData(2, "pipeline-2", "abc,pqr,xyz", 3)]
    public async Task AddNamedMessageProcessingPipeline_ShouldWorkCorrectly(int numberOfBackgroundServices, string pipelineName, string messages, int countOfMessages)
    {
        var mocks = new TestMocks();

        IHostBuilder hostBuilder = FakeHost.CreateBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            mocks.RegisterCommonServices(services);

            MeasureToken overallSuccessToken = new("overallSuccess", 0);
            MeasureToken overallFailureToken = new("overallFailure", 0);

            MeasureToken delegateSuccessToken = new("delegateSuccess", 1);
            MeasureToken delegateFailureToken = new("delegateFailure", 1);

            // Create a message consumer pipeline builder.
            var builder = services.AddAsyncPipeline(pipelineName)
                                  .ConfigureMessageSource<IMessageSource>(_ => new SampleSource(TestMocks.GetMessages(messages)))
                                  .AddLatencyContextMiddleware()
                                  .AddLatencyRecorderMessageMiddleware(overallSuccessToken, overallFailureToken)
                                  .AddMessageMiddleware(_ => new SampleMiddleware(mocks.MockDelegate.Object))
                                  .AddLatencyRecorderMessageMiddleware(delegateSuccessToken, delegateFailureToken)
                                  .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(mocks.MockMessageDestination.Object))
                                  .ConfigureMessageConsumer(sp =>
                                  {
                                      var messageSource = sp.GetRequiredService<INamedServiceProvider<IMessageSource>>().GetRequiredService(pipelineName);
                                      var messageDelegate = sp.GetRequiredService<INamedServiceProvider<IMessageDelegate>>().GetRequiredService(pipelineName);
                                      return new SampleConsumer(messageSource, messageDelegate);
                                  });

            // Run multiple background services for message consumer.
            for (int i = 0; i < numberOfBackgroundServices; i++)
            {
                builder.RunConsumerAsBackgroundService();
            }
        });

        // Build the host.
        using IHost host = hostBuilder.Build();

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
            mocks.MockLatencyContextProvider.Verify(x => x.CreateContext(), Times.Exactly(countOfMessages));
            mocks.MockLatencyContext.Verify(x => x.AddMeasure(It.IsAny<MeasureToken>(), It.IsAny<long>()), Times.Exactly(2 * countOfMessages));
            mocks.MockLatencyContext.Verify(x => x.Freeze(), Times.Exactly(countOfMessages));
            mocks.MockLatencyDataExporter.Verify(x => x.ExportAsync(It.IsAny<LatencyData>(), It.IsAny<CancellationToken>()), Times.Exactly(countOfMessages));
        }
        else
        {
            mocks.VerifyMocksHavingCountAtleast(countOfMessages);
            mocks.MockLatencyContextProvider.Verify(x => x.CreateContext(), Times.AtLeast(countOfMessages));
            mocks.MockLatencyContext.Verify(x => x.AddMeasure(It.IsAny<MeasureToken>(), It.IsAny<long>()), Times.AtLeast(2 * countOfMessages));
            mocks.MockLatencyContext.Verify(x => x.Freeze(), Times.AtLeast(countOfMessages));
            mocks.MockLatencyDataExporter.Verify(x => x.ExportAsync(It.IsAny<LatencyData>(), It.IsAny<CancellationToken>()), Times.AtLeast(countOfMessages));
        }
    }

    [Theory]
    [InlineData("pipeline-", "abc,pqr,xyz", 6)]
    public async Task AddMultipleNamedMessageProcessingPipeline_ShouldWorkCorrectly(string pipelineNamePrefix, string messages, int countOfMessages)
    {
        var mocks = new TestMocks();

        IHostBuilder hostBuilder = FakeHost.CreateBuilder();
        hostBuilder.ConfigureServices(services =>
        {
            mocks.RegisterCommonServices(services);

            // Create a message consumer pipeline.
            string pipelineName1 = pipelineNamePrefix + "1";
            services.AddAsyncPipeline(pipelineName1)
                    .ConfigureMessageSource<IMessageSource>(_ => new SampleSource(TestMocks.GetMessages(messages)))
                    .AddMessageMiddleware(_ => new SampleMiddleware(mocks.MockDelegate.Object))
                    .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(mocks.MockMessageDestination.Object))
                    .ConfigureMessageConsumer(sp =>
                    {
                        var messageSource = sp.GetRequiredService<INamedServiceProvider<IMessageSource>>().GetRequiredService(pipelineName1);
                        var messageDelegate = sp.GetRequiredService<INamedServiceProvider<IMessageDelegate>>().GetRequiredService(pipelineName1);
                        return new SampleConsumer(messageSource, messageDelegate);
                    })
                    .RunConsumerAsBackgroundService();

            // Create another message consumer pipeline.
            string pipelineName2 = pipelineNamePrefix + "2";
            services.AddAsyncPipeline(pipelineName2)
                    .ConfigureMessageSource<IMessageSource>(_ => new AnotherSource(TestMocks.GetMessages(messages)))
                    .AddMessageMiddleware(_ => new SampleMiddleware(mocks.MockDelegate.Object))
                    .ConfigureTerminalMessageDelegate(_ => new SampleWriterDelegate(mocks.MockMessageDestination.Object))
                    .ConfigureMessageConsumer(sp =>
                    {
                        var messageSource = sp.GetRequiredService<INamedServiceProvider<IMessageSource>>().GetRequiredService(pipelineName2);
                        var messageDelegate = sp.GetRequiredService<INamedServiceProvider<IMessageDelegate>>().GetRequiredService(pipelineName2);
                        return new OverridenConsumer(messageSource, messageDelegate, sp.GetRequiredService<ILogger>());
                    })
                    .RunConsumerAsBackgroundService();
        });

        // Build the host.
        using IHost host = hostBuilder.Build();

        // Validate the hosted services include ConsumerBackgroundServices.
        var hostedServices = host.Services.GetServices<IHostedService>().ToList();
        int consumerServicesCount = hostedServices.Count(x => x is ConsumerBackgroundService);
        Assert.Equal(2, consumerServicesCount);

        // Start and stop the host.
        await host.StartAsync();
        await host.StopAsync();

        // Verify Mocks
        mocks.VerifyMocksHavingCount(countOfMessages);
    }

    private class TestMocks
    {
        public Mock<IMessageDestination> MockMessageDestination = new();
        public Mock<IMessageDelegate> MockDelegate = new();
        public Mock<ILatencyContextProvider> MockLatencyContextProvider = new();
        public Mock<ILatencyContext> MockLatencyContext = new();
        public Mock<ILatencyDataExporter> MockLatencyDataExporter = new();
        public ILogger Logger = new FakeLogger();

        public TestMocks()
        {
            // Setup Mocks
            MockMessageDestination.Setup(x => x.WriteAsync(It.IsAny<MessageContext>())).Returns(new ValueTask(Task.CompletedTask));
            MockDelegate.Setup(x => x.InvokeAsync(It.IsAny<MessageContext>())).Returns(new ValueTask(Task.CompletedTask));
            MockLatencyContextProvider.Setup(x => x.CreateContext()).Returns(MockLatencyContext.Object);
        }

        public static string[] GetMessages(string csv)
        {
            return csv.Split(',');
        }

        public void RegisterCommonServices(IServiceCollection services)
        {
            // Register logger.
            services.TryAddSingleton(Logger);

            // Register latency context provider.
            services.TryAddSingleton(MockLatencyContextProvider.Object);

            // Register exporters.
            services.AddSingleton(MockLatencyDataExporter.Object);
        }

        public void VerifyMocksHavingCount(int count)
        {
            MockDelegate.Verify(x => x.InvokeAsync(It.IsAny<MessageContext>()), Times.Exactly(count));
            MockMessageDestination.Verify(x => x.WriteAsync(It.IsAny<MessageContext>()), Times.Exactly(count));
        }

        public void VerifyMocksHavingCountAtleast(int count)
        {
            MockDelegate.Verify(x => x.InvokeAsync(It.IsAny<MessageContext>()), Times.AtLeast(count));
            MockMessageDestination.Verify(x => x.WriteAsync(It.IsAny<MessageContext>()), Times.AtLeast(count));
        }
    }
}
