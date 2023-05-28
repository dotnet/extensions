// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Middlewares.Tests.Data.Consumers;
using System.Cloud.Messaging.Middlewares.Tests.Data.Middlewares;
using System.Cloud.Messaging.Middlewares.Tests.Data.Sources;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Latency;
using Moq;
using Xunit;

namespace System.Cloud.Messaging.Middlewares.Tests.Extensions;

/// <summary>
/// Tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData("a,b,c", 1, "pipeline-1")]
    [InlineData("p,q,r", 2, "pipeline-2")]
    public async Task AddNamedMessageProcessingPipeline_ShouldWorkCorrectly(string csvMessage, int numberOfBackgroundServices, string pipelineName)
    {
        var mocks = new TestMocks();

        IHostBuilder hostBuilder = FakeHost.CreateBuilder(TestMocks.GetFakeHostOptions());
        hostBuilder.ConfigureServices(services =>
        {
            mocks.RegisterCommonServices(services);

            MeasureToken overallSuccessToken = new("overallSuccess", 0);
            MeasureToken overallFailureToken = new("overallFailure", 0);

            MeasureToken delegateSuccessToken = new("delegateSuccess", 1);
            MeasureToken delegateFailureToken = new("delegateFailure", 1);

            // Create a message consumer pipeline builder.
            var builder = services.AddAsyncPipeline(pipelineName)
                                  .ConfigureMessageSource(_ => new SampleSource(csvMessage.Split(',')))
                                  .AddLatencyContextMiddleware()
                                  .AddLatencyRecorderMessageMiddleware(overallSuccessToken, overallFailureToken)
                                  .AddMessageMiddleware<SampleMiddleware>()
                                  .AddLatencyRecorderMessageMiddleware(delegateSuccessToken, delegateFailureToken)
                                  .ConfigureTerminalMessageDelegate(_ => mocks.MockMessageDelegate.Object)
                                  .ConfigureMessageConsumer(sp => new SingleMessageConsumer(sp.GetMessageSource(pipelineName),
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

        // Start and stop the host.
        await host.StartAsync();
        await host.StopAsync();

        // Verify Mocks
        mocks.VerifyMocksHavingCount(numberOfBackgroundServices);
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

        public Mock<MessageDelegate> MockMessageDelegate = new();
        public Mock<ILatencyContextProvider> MockLatencyContextProvider = new();
        public Mock<ILatencyContext> MockLatencyContext = new();
        public Mock<ILatencyDataExporter> MockLatencyDataExporter = new();

        public TestMocks()
        {
            // Setup Mocks
            MockMessageDelegate.Setup(x => x.Invoke(It.IsAny<MessageContext>())).Returns(new ValueTask(Task.CompletedTask));
            MockLatencyContextProvider.Setup(x => x.CreateContext()).Returns(MockLatencyContext.Object);
        }

        public void RegisterCommonServices(IServiceCollection services)
        {
            // Register middleware.
            services.TryAddSingleton<SampleMiddleware>();

            // Register latency context provider.
            services.TryAddSingleton(MockLatencyContextProvider.Object);

            // Register exporters.
            services.AddSingleton(MockLatencyDataExporter.Object);
        }

        public void VerifyMocksHavingCount(int count)
        {
            MockMessageDelegate.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Exactly(count));
            MockLatencyContextProvider.Verify(x => x.CreateContext(), Times.Exactly(count));
            MockLatencyContext.Verify(x => x.AddMeasure(It.IsAny<MeasureToken>(), It.IsAny<long>()), Times.Exactly(2 * count));
            MockLatencyContext.Verify(x => x.Freeze(), Times.Exactly(count));
            MockLatencyDataExporter.Verify(x => x.ExportAsync(It.IsAny<LatencyData>(), It.IsAny<CancellationToken>()), Times.Exactly(count));
        }
    }
}
