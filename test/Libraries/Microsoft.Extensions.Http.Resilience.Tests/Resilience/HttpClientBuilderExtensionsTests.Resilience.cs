// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Moq;
using Polly;
using Polly.Extensions.Telemetry;
using Polly.Registry;
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
        Assert.Throws<ArgumentNullException>(() => builder.AddResilienceHandler("dummy", (Action<ResilienceStrategyBuilder<HttpResponseMessage>>)null!));
        Assert.Throws<ArgumentNullException>(() => builder.AddResilienceHandler("dummy", (Action<ResilienceStrategyBuilder<HttpResponseMessage>, ResilienceHandlerContext>)null!));

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

        Assert.Contains(services, s => s.ServiceType == typeof(ResilienceStrategyProvider<HttpKey>));
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

        builder.Services.Should().HaveCount(count + 2);
    }

    [Fact]
    public async Task AddResilienceHandler_EnsureFailureResultContext()
    {
        using var metricCollector = new MetricCollector<int>(null, "Polly", "resilience-events");

        var asserted = false;
        var services = new ServiceCollection()
            .AddResilienceEnrichment()
            .Configure<TelemetryOptions>(options =>
            {
                options.Enrichers.Add(context =>
                {
                    var dict = context.Tags.ToDictionary(t => t.Key, t => t.Value);
                    dict.Should().ContainKey("failure-reason").WhoseValue.Should().Be("500");
                    dict.Should().ContainKey("failure-summary").WhoseValue.Should().Be("InternalServerError");
                    dict.Should().ContainKey("failure-source").WhoseValue.Should().Be(TelemetryConstants.Unknown);

                    asserted = true;
                });
            });

        var clientBuilder = services
            .AddHttpClient("client")
            .ConfigurePrimaryHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.InternalServerError))
            .AddStandardResilienceHandler()
            .Configure(options =>
            {
                options.RetryOptions.ShouldHandle = _ => PredicateResult.True;
                options.RetryOptions.RetryCount = 1;
                options.RetryOptions.BaseDelay = TimeSpan.Zero;
            });

        var client = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("client");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://dummy");

        using var response = await client.SendAsync(request);

        asserted.Should().BeTrue();
    }

    [Fact]
    public async Task AddResilienceHandler_EnsureResilienceHandlerContext()
    {
        var verified = false;
        _builder
            .AddResilienceHandler("test", (_, context) =>
            {
                context.ServiceProvider.Should().NotBeNull();
                context.BuilderName.Should().Be($"{BuilderName}-test");
                context.StrategyKey.Should().Be("dummy-key");
                verified = true;
            })
            .SelectStrategyBy(_ => _ => "dummy-key");

        _builder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.InternalServerError));

        await CreateClient(BuilderName).GetAsync("https://dummy");
        verified.Should().BeTrue();
    }

    [Fact]
    public void AddResilienceHandler_EnsureCorrectRegistryOptions()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder? builder = services.AddHttpClient("client");
        builder.AddResilienceHandler("test", ConfigureBuilder);

        var registryOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<ResilienceStrategyRegistryOptions<HttpKey>>>().Value;
        registryOptions.BuilderComparer.Equals(new HttpKey("A", "1"), new HttpKey("A", "2")).Should().BeTrue();
        registryOptions.BuilderComparer.Equals(new HttpKey("A", "1"), new HttpKey("B", "1")).Should().BeFalse();

        registryOptions.StrategyComparer.Equals(new HttpKey("A", "1"), new HttpKey("A", "1")).Should().BeTrue();
        registryOptions.StrategyComparer.Equals(new HttpKey("A", "1"), new HttpKey("A", "2")).Should().BeFalse();

        registryOptions.BuilderNameFormatter(new HttpKey("A", "1")).Should().Be("A");
        registryOptions.StrategyKeyFormatter(new HttpKey("A", "1")).Should().Be("1");
    }

    public enum PolicyType
    {
        Fallback,
        Retry,
        CircuitBreaker,
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task AddResilienceHandler_EnsureProperStrategyInstanceRetrieved(bool bySelector)
    {
        // arrange
        var resilienceProvider = new Mock<ResilienceStrategyProvider<HttpKey>>(MockBehavior.Strict);
        var services = new ServiceCollection().AddLogging().RegisterMetering().AddFakeRedaction();
        services.AddSingleton(resilienceProvider.Object);
        var builder = services.AddHttpClient("client");
        var pipelineBuilder = builder.AddResilienceHandler("dummy", ConfigureBuilder);
        var expectedStrategyName = "client-dummy";
        if (bySelector)
        {
            pipelineBuilder.SelectStrategyByAuthority(DataClassification.Unknown);
        }

        builder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        var provider = services.BuildServiceProvider();
        if (bySelector)
        {
            resilienceProvider
                .Setup(v => v.GetStrategy<HttpResponseMessage>(new HttpKey(expectedStrategyName, "https://dummy1")))
                .Returns(NullResilienceStrategy<HttpResponseMessage>.Instance);
        }
        else
        {
            resilienceProvider
                .Setup(v => v.GetStrategy<HttpResponseMessage>(new HttpKey(expectedStrategyName, string.Empty)))
                .Returns(NullResilienceStrategy<HttpResponseMessage>.Instance);
        }

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("client");

        // act
        await client.GetAsync("https://dummy1");

        // assert
        resilienceProvider.VerifyAll();
    }

    [Fact]
    public async Task AddResilienceHandlerBySelector_EnsureResilienceStrategyProviderCalled()
    {
        // arrange
        var services = new ServiceCollection().AddLogging().RegisterMetering();
        var providerMock = new Mock<ResilienceStrategyProvider<HttpKey>>(MockBehavior.Strict);

        services.AddSingleton(providerMock.Object);
        var pipelineName = string.Empty;

        pipelineName = "client-my-pipeline";
        var clientBuilder = services.AddHttpClient("client");
        clientBuilder.AddResilienceHandler("my-pipeline", ConfigureBuilder);
        clientBuilder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        providerMock
            .Setup(v => v.GetStrategy<HttpResponseMessage>(new HttpKey(pipelineName, string.Empty)))
            .Returns(NullResilienceStrategy<HttpResponseMessage>.Instance)
            .Verifiable();

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("client");
        var pipelineProvider = provider.GetRequiredService<ResilienceStrategyProvider<HttpKey>>();

        // act
        await client.GetAsync("https://dummy1");

        // assert
        providerMock.VerifyAll();
    }

    [Fact]
    public void AddResilienceHandler_AuthoritySelectorAndNotConfiguredRedaction_EnsureValidated()
    {
        // arrange
        var clientBuilder = new ServiceCollection().AddLogging().RegisterMetering().AddRedaction()
            .AddHttpClient("my-client")
            .AddResilienceHandler("my-pipeline", ConfigureBuilder)
            .SelectStrategyByAuthority(SimpleClassifications.PrivateData);

        var factory = clientBuilder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient("my-client"));
        Assert.Equal("The redacted strategy key is an empty string and cannot be used for the strategy selection. Is redaction correctly configured?", error.Message);
    }

    [Fact]
    public void AddResilienceHandler_AuthorityByCustomSelector_NotValidated()
    {
        // arrange
        var clientBuilder = new ServiceCollection().AddLogging().RegisterMetering().AddRedaction()
            .AddHttpClient("my-client")
            .AddResilienceHandler("my-pipeline", ConfigureBuilder)
            .SelectStrategyBy(_ => _ => string.Empty);

        var factory = clientBuilder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        Assert.NotNull(factory.CreateClient("my-client"));
    }

    private void ConfigureBuilder(ResilienceStrategyBuilder<HttpResponseMessage> builder) => builder.AddTimeout(TimeSpan.FromSeconds(1));
}
