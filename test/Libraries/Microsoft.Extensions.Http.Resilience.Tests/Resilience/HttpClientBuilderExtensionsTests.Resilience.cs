// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Registry;
using Polly.Telemetry;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test;

public sealed partial class HttpClientBuilderExtensionsTests
{
    [Fact]
    public void AddResilienceHandler_ArgumentValidation()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder? builder = services.AddHttpClient("client");

        Assert.Throws<ArgumentNullException>(() => builder.AddResilienceHandler(null!, _ => { }));
        Assert.Throws<ArgumentException>(() => builder.AddResilienceHandler(string.Empty, _ => { }));
        Assert.Throws<ArgumentNullException>(() => builder.AddResilienceHandler(null!, (_, _) => { }));
        Assert.Throws<ArgumentException>(() => builder.AddResilienceHandler(string.Empty, (_, _) => { }));
        Assert.Throws<ArgumentNullException>(() => builder.AddResilienceHandler("dummy", (Action<ResiliencePipelineBuilder<HttpResponseMessage>>)null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddResilienceHandler("dummy", (Action<ResiliencePipelineBuilder<HttpResponseMessage>, ResilienceHandlerContext>)null!));

        builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddResilienceHandler("pipeline-name", _ => { }));
        Assert.Throws<ArgumentNullException>(() => builder!.AddResilienceHandler("pipeline-name", (_, _) => { }));
    }

    [Fact]
    public void AddResilienceHandler_EnsureCorrectServicesRegistered()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder? builder = services.AddHttpClient("client");

        builder.AddResilienceHandler("test", ConfigureBuilder);

        // add twice intentionally
        builder.AddResilienceHandler("test", ConfigureBuilder);

        Assert.Contains(services, s => s.ServiceType == typeof(ResiliencePipelineProvider<HttpKey>));
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
#endif
    public async Task AddResilienceHandler_OnPipelineDisposed_EnsureCalled(bool asynchronous = true)
    {
        var onPipelineDisposedCalled = false;
        var services = new ServiceCollection();
        IHttpClientBuilder? builder = services
            .AddHttpClient("client")
            .ConfigurePrimaryHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        builder.AddResilienceHandler("test", (builder, context) =>
        {
            builder.AddTimeout(TimeSpan.FromSeconds(1));
            context.OnPipelineDisposed(() => onPipelineDisposedCalled = true);
        });

        using (var serviceProvider = services.BuildServiceProvider())
        {
            var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("client");
            await SendRequest(client, "https://dummy", asynchronous);
        }

        onPipelineDisposedCalled.Should().BeTrue();
    }

    [Fact]
    public void AddResilienceHandler_EnsureServicesNotAddedTwice()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder? builder = services.AddHttpClient("client");

        builder.AddResilienceHandler("test", ConfigureBuilder);
        var count = builder.Services.Count;

        // add twice intentionally
        builder.AddResilienceHandler("test", ConfigureBuilder);

        // We check that the count of existing services is not unnecessary increased.
        //
        // The additional 3 services that are registered are related to:
        // - Configuration of HTTP client options
        // - Configuration of resilience pipeline
        // - Registration of keyed service for resilience pipeline
        // UPDATE NOTE: Starting from .NET 8.0.2, the count of additional services is 2 instead of 3. This is due to the fact that the registration of the resilience
        // pipeline is now done in the `AddResilienceHandler` method.
        builder.Services.Should().HaveCount(count + 2);
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
#endif
    public async Task AddResilienceHandler_EnsureErrorType(bool asynchronous = true)
    {
        using var metricCollector = new MetricCollector<int>(null, "Polly", "resilience.polly.strategy.events");
        var enricher = new TestMetricsEnricher();
        var clientBuilder = new ServiceCollection()
            .AddHttpClient("client")
            .ConfigurePrimaryHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.InternalServerError))
            .AddStandardResilienceHandler()
            .Configure(options =>
            {
                options.Retry.ShouldHandle = _ => PredicateResult.True();
                options.Retry.MaxRetryAttempts = 1;
                options.Retry.Delay = TimeSpan.Zero;
            });

        clientBuilder.Services.Configure<TelemetryOptions>(o => o.MeteringEnrichers.Add(enricher));

        var client = clientBuilder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("client");

        using var response = await SendRequest(client, "https://dummy", asynchronous);

        enricher.Tags["error.type"].Should().BeOfType<string>().Subject.Should().Be("500");
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
#endif
    public async Task AddResilienceHandler_EnsureResilienceHandlerContext(bool asynchronous = true)
    {
        var verified = false;
        _builder
            .AddResilienceHandler("test", (_, context) =>
            {
                context.ServiceProvider.Should().NotBeNull();
                context.BuilderName.Should().Be($"{BuilderName}-test");
                context.InstanceName.Should().Be("dummy-key");
                verified = true;
            })
            .SelectPipelineBy(_ => _ => "dummy-key");

        _builder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.InternalServerError));

        var client = CreateClient(BuilderName);
        await SendRequest(client, "https://dummy", asynchronous);

        verified.Should().BeTrue();
    }

    [Fact]
    public void AddResilienceHandler_EnsureCorrectRegistryOptions()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder? builder = services.AddHttpClient("client");
        builder.AddResilienceHandler("test", ConfigureBuilder);

        using var serviceProvider = builder.Services.BuildServiceProvider();
        var registryOptions = serviceProvider.GetRequiredService<IOptions<ResiliencePipelineRegistryOptions<HttpKey>>>().Value;
        registryOptions.BuilderComparer.Equals(new HttpKey("A", "1"), new HttpKey("A", "2")).Should().BeTrue();
        registryOptions.BuilderComparer.Equals(new HttpKey("A", "1"), new HttpKey("B", "1")).Should().BeFalse();

        registryOptions.PipelineComparer.Equals(new HttpKey("A", "1"), new HttpKey("A", "1")).Should().BeTrue();
        registryOptions.PipelineComparer.Equals(new HttpKey("A", "1"), new HttpKey("A", "2")).Should().BeFalse();

        registryOptions.BuilderNameFormatter(new HttpKey("A", "1")).Should().Be("A");
        registryOptions.InstanceNameFormatter!(new HttpKey("A", "1")).Should().Be("1");
    }

    public enum PolicyType
    {
        Fallback,
        Retry,
        CircuitBreaker,
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
    [InlineData(false)]
#endif
    public async Task AddResilienceHandler_EnsureProperPipelineInstanceRetrieved(bool bySelector, bool asynchronous = true)
    {
        // arrange
        var resilienceProvider = new Mock<ResiliencePipelineProvider<HttpKey>>(MockBehavior.Strict);
        var services = new ServiceCollection().AddLogging().AddMetrics().AddFakeRedaction();
        services.AddSingleton(resilienceProvider.Object);
        var builder = services.AddHttpClient("client");
        var pipelineBuilder = builder.AddResilienceHandler("dummy", ConfigureBuilder);
        var expectedPipelineKey = "client-dummy";
        if (bySelector)
        {
            pipelineBuilder.SelectPipelineByAuthority();
        }

        builder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        using var provider = services.BuildServiceProvider();
        if (bySelector)
        {
            resilienceProvider
                .Setup(v => v.GetPipeline<HttpResponseMessage>(new HttpKey(expectedPipelineKey, "https://dummy1")))
                .Returns(ResiliencePipeline<HttpResponseMessage>.Empty);
        }
        else
        {
            resilienceProvider
                .Setup(v => v.GetPipeline<HttpResponseMessage>(new HttpKey(expectedPipelineKey, string.Empty)))
                .Returns(ResiliencePipeline<HttpResponseMessage>.Empty);
        }

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("client");

        // act
        await SendRequest(client, "https://dummy1", asynchronous);

        // assert
        resilienceProvider.VerifyAll();
    }

    [Theory]
#if NET6_0_OR_GREATER
    [CombinatorialData]
#else
    [InlineData(true)]
#endif
    public async Task AddResilienceHandlerBySelector_EnsureResiliencePipelineProviderCalled(bool asynchronous = true)
    {
        // arrange
        var services = new ServiceCollection().AddLogging().AddMetrics();
        var providerMock = new Mock<ResiliencePipelineProvider<HttpKey>>(MockBehavior.Strict);

        services.AddSingleton(providerMock.Object);
        var pipelineName = string.Empty;

        pipelineName = "client-my-pipeline";
        var clientBuilder = services.AddHttpClient("client");
        clientBuilder.AddResilienceHandler("my-pipeline", ConfigureBuilder);
        clientBuilder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        providerMock
            .Setup(v => v.GetPipeline<HttpResponseMessage>(new HttpKey(pipelineName, string.Empty)))
            .Returns(ResiliencePipeline<HttpResponseMessage>.Empty)
            .Verifiable();

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("client");
        var pipelineProvider = provider.GetRequiredService<ResiliencePipelineProvider<HttpKey>>();

        // act
        await SendRequest(client, "https://dummy1", asynchronous);

        // assert
        providerMock.VerifyAll();
    }

    [Fact]
    public void AddResilienceHandler_AuthorityByCustomSelector_NotValidated()
    {
        // arrange
        var clientBuilder = new ServiceCollection().AddLogging().AddMetrics().AddRedaction()
            .AddHttpClient("my-client")
            .AddResilienceHandler("my-pipeline", ConfigureBuilder)
            .SelectPipelineBy(_ => _ => string.Empty);

        using var serviceProvider = clientBuilder.Services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        Assert.NotNull(factory.CreateClient("my-client"));
    }

    private void ConfigureBuilder(ResiliencePipelineBuilder<HttpResponseMessage> builder) => builder.AddTimeout(TimeSpan.FromSeconds(1));

    private class TestMetricsEnricher : MeteringEnricher
    {
        public Dictionary<string, object?> Tags { get; } = [];

        public override void Enrich<TResult, TArgs>(in EnrichmentContext<TResult, TArgs> context)
        {
            foreach (var tag in context.Tags)
            {
                Tags[tag.Key] = tag.Value;
            }
        }
    }
}
