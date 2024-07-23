// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Hedging;
using Polly.Registry;
using Polly.Testing;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

public sealed class StandardHedgingTests : HedgingTests<IStandardHedgingHandlerBuilder>
{
    public StandardHedgingTests()
        : base(ConfigureDefaultBuilder)
    {
    }

    private static IStandardHedgingHandlerBuilder ConfigureDefaultBuilder(IHttpClientBuilder builder, Func<RequestRoutingStrategy> factory)
    {
        return builder
            .AddStandardHedgingHandler(routing => routing.ConfigureRoutingStrategy(_ => factory))
            .Configure(options =>
            {
                options.Hedging.MaxHedgedAttempts = DefaultHedgingAttempts;
                options.Hedging.Delay = TimeSpan.FromMilliseconds(5);
            });
    }

    [Fact]
    public void EnsureValidated_BasicValidation()
    {
        Builder.Configure(options => options.Hedging.MaxHedgedAttempts = -1);

        Assert.Throws<OptionsValidationException>(CreateClientWithHandler);
    }

    [Fact]
    public void EnsureValidated_AdvancedValidation()
    {
        Builder.Configure(options => options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(1));

        Assert.Throws<OptionsValidationException>(CreateClientWithHandler);
    }

    [Fact]
    public void Configure_Callback_Ok()
    {
        Builder.Configure(o => o.Hedging.MaxHedgedAttempts = 8);

        using var serviceProvider = Builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);

        Assert.Equal(8, options.Hedging.MaxHedgedAttempts);
    }

    [Fact]
    public void Configure_CallbackWithServiceProvider_Ok()
    {
        Builder.Configure((o, serviceProvider) =>
        {
            serviceProvider.GetRequiredService<ResiliencePipelineProvider<HttpKey>>().Should().NotBeNull();
            o.Hedging.MaxHedgedAttempts = 8;
        });

        using var serviceProvider = Builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);

        Assert.Equal(8, options.Hedging.MaxHedgedAttempts);
    }

    [Fact]
    public void RoutingStrategyBuilder_EnsureExpectedName()
    {
        Assert.Equal(ClientId, Builder.RoutingStrategyBuilder.Name);
    }

    [Fact]
    public void Configure_ValidConfigurationSection_ShouldInitialize()
    {
        var section = ConfigurationStubFactory.Create(new Dictionary<string, string?>
        {
            { "dummy:Hedging:MaxHedgedAttempts", "8" }
        }).GetSection("dummy");

        Builder.Configure(section);

        using var serviceProvider = Builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);

        Assert.Equal(8, options.Hedging.MaxHedgedAttempts);
    }

    [Fact]
    public void ActionGenerator_Ok()
    {
        using var serviceProvider = Builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);
        var generator = options.Hedging.ActionGenerator;
        var primary = ResilienceContextPool.Shared.Get();
        var secondary = ResilienceContextPool.Shared.Get();
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        var args = new HedgingActionGeneratorArguments<HttpResponseMessage>(primary, secondary, 0, _ => Outcome.FromResultAsValueTask(response));
        generator.Invoking(g => g(args)).Should().Throw<InvalidOperationException>().WithMessage("Request message snapshot is not attached to the resilience context.");

        using var request = new HttpRequestMessage();
        using var snapshot = RequestMessageSnapshot.Create(request);
        primary.Properties.Set(ResilienceKeys.RequestSnapshot, snapshot);
        generator.Invoking(g => g(args)).Should().NotThrow();
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void Configure_InvalidConfigurationSection_ShouldThrow()
    {
        var section = ConfigurationStubFactory.Create(new Dictionary<string, string?>
        {
            { "dummy:HedgingOptionsTypo:MaxHedgedAttempts", "8" },
            { "dummy:TotalRequestTimeoutOptions:TimeoutInterval", "00:00:20" },
        }).GetSection("dummy");

        Builder.Configure(section);

        Assert.Throws<InvalidOperationException>(() =>
        {
            using var serviceProvider = Builder.Services.BuildServiceProvider();
            return serviceProvider
                    .GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>()
                    .Get(Builder.Name);
        });
    }
#endif

    [Fact]
    public void Configure_EmptyConfigurationSectionContent_ShouldThrow()
    {
        var section = ConfigurationStubFactory.Create(new Dictionary<string, string?>
        {
            { "dummy", "" }
        }).GetSection("dummy");

        Assert.Throws<ArgumentException>(() =>
            Builder.Configure(section));
    }

    [Fact]
    public void Configure_EmptyConfigurationSection_ShouldThrow()
    {
        var section = ConfigurationStubFactory.CreateEmpty().GetSection(string.Empty);

        Assert.Throws<ArgumentException>(() =>
            Builder.Configure(section));
    }

    [Fact]
    public void VerifyPipeline()
    {
        using var serviceProvider = Builder.Services.BuildServiceProvider();
        var pipelineProvider = serviceProvider.GetRequiredService<ResiliencePipelineProvider<HttpKey>>();

        // primary handler
        var primary = pipelineProvider.GetPipeline<HttpResponseMessage>(new HttpKey("clientId-standard-hedging", "instance")).GetPipelineDescriptor();
        primary.IsReloadable.Should().BeTrue();
        primary.Strategies.Should().HaveCount(4);
        primary.Strategies[0].StrategyInstance.Should().BeOfType<RoutingResilienceStrategy>();
        primary.Strategies[1].StrategyInstance.Should().BeOfType<RequestMessageSnapshotStrategy>();
        primary.Strategies[2].Options.Should().BeOfType<HttpTimeoutStrategyOptions>();
        primary.Strategies[3].Options.Should().BeOfType<HttpHedgingStrategyOptions>();

        // inner handler
        var inner = pipelineProvider.GetPipeline<HttpResponseMessage>(new HttpKey("clientId-standard-hedging-endpoint", "instance")).GetPipelineDescriptor();
        inner.IsReloadable.Should().BeTrue();
        inner.Strategies.Should().HaveCount(3);
        inner.Strategies[0].Options.Should().BeOfType<HttpRateLimiterStrategyOptions>();
        inner.Strategies[1].Options.Should().BeOfType<HttpCircuitBreakerStrategyOptions>();
        inner.Strategies[2].Options.Should().BeOfType<HttpTimeoutStrategyOptions>();
    }

    [InlineData(null)]
    [InlineData("custom-key")]
    [Theory]
    public async Task VerifyPipelineSelection(string? customKey)
    {
        var noPolicy = ResiliencePipeline<HttpResponseMessage>.Empty;
        var provider = new Mock<ResiliencePipelineProvider<HttpKey>>(MockBehavior.Strict);
        Builder.Services.AddSingleton(provider.Object);
        if (customKey == null)
        {
            Builder.SelectPipelineByAuthority();
        }
        else
        {
            Builder.SelectPipelineBy(_ => _ => customKey);
        }

        customKey ??= "https://key:80";
        provider.Setup(v => v.GetPipeline<HttpResponseMessage>(new HttpKey("clientId-standard-hedging", string.Empty))).Returns(noPolicy);
        provider.Setup(v => v.GetPipeline<HttpResponseMessage>(new HttpKey("clientId-standard-hedging-endpoint", customKey))).Returns(noPolicy);

        using var client = CreateClientWithHandler();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://key:80/discarded");
        AddResponse(HttpStatusCode.OK);

        using var response = await client.SendAsync(request, CancellationToken.None);

        provider.VerifyAll();
    }

    [Fact]
    public async Task DynamicReloads_Ok()
    {
        // arrange
        var requests = new List<HttpRequestMessage>();
        var config = ConfigurationStubFactory.Create(
            new()
            {
                { "standard:Hedging:MaxHedgedAttempts", "2" }
            },
            out var reloadAction).GetSection("standard");

        Builder.Configure(config).Configure(options => options.Hedging.Delay = Timeout.InfiniteTimeSpan);
        SetupRouting();
        SetupRoutes(10);

        var client = CreateClientWithHandler();

        // act && assert
        AddResponse(HttpStatusCode.InternalServerError, 3);
        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        using var _ = await client.SendAsync(firstRequest);
        AssertNoResponse();

        reloadAction(new() { { "standard:Hedging:MaxHedgedAttempts", "6" } });

        AddResponse(HttpStatusCode.InternalServerError, 7);
        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        using var __ = await client.SendAsync(secondRequest);
        AssertNoResponse();
    }

    [Fact]
    public async Task NoRouting_Ok()
    {
        // arrange
        Builder.Services.Configure<RequestRoutingOptions>(Builder.RoutingStrategyBuilder.Name, options => options.RoutingStrategyProvider = null);

        var client = CreateClientWithHandler();

        // act && assert
        AddResponse(HttpStatusCode.InternalServerError, 3);
        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "https://some-endpoint:1234/some-path?query");
        using var _ = await client.SendAsync(firstRequest);
        AssertNoResponse();

        Requests.Should().AllSatisfy(r => r.Should().Be("https://some-endpoint:1234/some-path?query"));
    }

    [Fact]
    public async Task SendAsync_FailedConnect_ShouldReturnResponseFromHedging()
    {
        const string FailingEndpoint = "www.failing-host.com";

        var services = new ServiceCollection();
        _ = services
            .AddHttpClient(ClientId)
            .ConfigurePrimaryHttpMessageHandler(() => new MockHttpMessageHandler(FailingEndpoint))
            .AddStandardHedgingHandler(routing =>
                routing.ConfigureOrderedGroups(g =>
                {
                    g.Groups.Add(new UriEndpointGroup
                    {
                        Endpoints = [new WeightedUriEndpoint { Uri = new Uri($"https://{FailingEndpoint}:3000") }]
                    });

                    g.Groups.Add(new UriEndpointGroup
                    {
                        Endpoints = [new WeightedUriEndpoint { Uri = new Uri("https://microsoft.com") }]
                    });
                }))
            .Configure(opt =>
            {
                opt.Hedging.MaxHedgedAttempts = 10;
                opt.Hedging.Delay = TimeSpan.FromSeconds(11);
                opt.Endpoint.CircuitBreaker.FailureRatio = 0.99;
                opt.Endpoint.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(900);
                opt.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(200);
                opt.Endpoint.Timeout.Timeout = TimeSpan.FromSeconds(200);
            });

        await using var provider = services.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
        using var client = clientFactory.CreateClient(ClientId);

        var ex = await Record.ExceptionAsync(async () =>
        {
            using var _ = await client.GetAsync($"https://{FailingEndpoint}:3000");
        });

        Assert.Null(ex);
    }

    protected override void ConfigureHedgingOptions(Action<HttpHedgingStrategyOptions> configure) => Builder.Configure(options => configure(options.Hedging));

    private class MockHttpMessageHandler(string failingEndpoint) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.Host == failingEndpoint)
            {
                await Task.Delay(100, cancellationToken);
                throw new OperationCanceledExceptionMock(new TimeoutException());
            }

            await Task.Delay(1000, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
