﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Compliance.Testing;
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
                options.HedgingOptions.MaxHedgedAttempts = DefaultHedgingAttempts;
                options.HedgingOptions.HedgingDelay = TimeSpan.FromMilliseconds(5);
            });
    }

    [Fact]
    public void EnsureValidated_BasicValidation()
    {
        Builder.Configure(options => options.HedgingOptions.MaxHedgedAttempts = -1);

        Assert.Throws<OptionsValidationException>(() => CreateClientWithHandler());
    }

    [Fact]
    public void EnsureValidated_AdvancedValidation()
    {
        Builder.Configure(options => options.TotalRequestTimeoutOptions.Timeout = TimeSpan.FromSeconds(1));

        Assert.Throws<OptionsValidationException>(() => CreateClientWithHandler());
    }

    [Fact]
    public void Configure_Callback_Ok()
    {
        Builder.Configure(o => o.HedgingOptions.MaxHedgedAttempts = 8);

        var options = Builder.Services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);

        Assert.Equal(8, options.HedgingOptions.MaxHedgedAttempts);
    }

    [Fact]
    public void Configure_CallbackWithServiceProvider_Ok()
    {
        Builder.Configure((o, serviceProvider) =>
        {
            serviceProvider.GetRequiredService<ResilienceStrategyProvider<HttpKey>>().Should().NotBeNull();
            o.HedgingOptions.MaxHedgedAttempts = 8;
        });

        var options = Builder.Services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);

        Assert.Equal(8, options.HedgingOptions.MaxHedgedAttempts);
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
            { "dummy:HedgingOptions:MaxHedgedAttempts", "8" }
        }).GetSection("dummy");

        Builder.Configure(section);

        var options = Builder.Services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);

        Assert.Equal(8, options.HedgingOptions.MaxHedgedAttempts);
    }

    [Fact]
    public void ActionGenerator_Ok()
    {
        var options = Builder.Services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);
        var generator = options.HedgingOptions.HedgingActionGenerator;
        var primary = ResilienceContext.Get();
        var secondary = ResilienceContext.Get();
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        var args = new HedgingActionGeneratorArguments<HttpResponseMessage>(primary, secondary, 0, _ => Outcome.FromResultAsTask(response));
        generator.Invoking(g => g(args)).Should().Throw<InvalidOperationException>().WithMessage("Request message snapshot is not attached to the resilience context.");

        using var request = new HttpRequestMessage();
        using var snapshot = RequestMessageSnapshot.Create(request);
        primary.Properties.Set(ResilienceKeys.RequestSnapshot, snapshot);
        generator.Invoking(g => g(args)).Should().NotThrow();
    }

#if NET8_0_OR_GREATER
    // Whilst these API are marked as NET6_0_OR_GREATER we don't build .NET 6.0,
    // and as such the API is available in .NET 8 onwards.
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
            Builder.Services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>()
            .Get(Builder.Name));
    }
#endif

    [Fact]
    public void Configure_EmptyConfigurationSectionContent_ShouldThrow()
    {
        var section = ConfigurationStubFactory.Create(new Dictionary<string, string?>
        {
            { "dummy", "" }
        }).GetSection("dummy");

        Assert.Throws<ArgumentNullException>(() =>
            Builder.Configure(section));
    }

    [Fact]
    public void Configure_EmptyConfigurationSection_ShouldThrow()
    {
        var section = ConfigurationStubFactory.CreateEmpty().GetSection(string.Empty);

        Assert.Throws<ArgumentNullException>(() =>
            Builder.Configure(section));
    }

    [Fact]
    public void VerifyPipeline()
    {
        var serviceProvider = Builder.Services.BuildServiceProvider();
        var strategyProvider = serviceProvider.GetRequiredService<ResilienceStrategyProvider<HttpKey>>();

        // primary handler
        var primary = strategyProvider.GetStrategy<HttpResponseMessage>(new HttpKey("clientId-standard-hedging", "instance")).GetInnerStrategies();
        primary.HasTelemetry.Should().BeTrue();
        primary.IsReloadable.Should().BeTrue();
        primary.Strategies.Should().HaveCount(4);
        primary.Strategies[0].StrategyType.Should().Be(typeof(RoutingResilienceStrategy));
        primary.Strategies[1].StrategyType.Should().Be(typeof(RequestMessageSnapshotStrategy));
        primary.Strategies[2].Options.Should().BeOfType<HttpTimeoutStrategyOptions>();
        primary.Strategies[3].Options.Should().BeOfType<HttpHedgingStrategyOptions>();

        // inner handler
        var inner = strategyProvider.GetStrategy<HttpResponseMessage>(new HttpKey("clientId-standard-hedging-endpoint", "instance")).GetInnerStrategies();
        inner.HasTelemetry.Should().BeTrue();
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
        var noPolicy = NullResilienceStrategy<HttpResponseMessage>.Instance;
        var provider = new Mock<ResilienceStrategyProvider<HttpKey>>(MockBehavior.Strict);
        Builder.Services.AddSingleton(provider.Object);
        if (customKey == null)
        {
            Builder.SelectStrategyByAuthority(SimpleClassifications.PublicData);
        }
        else
        {
            Builder.SelectStrategyBy(_ => _ => customKey);
        }

        customKey ??= "https://key:80";
        provider.Setup(v => v.GetStrategy<HttpResponseMessage>(new HttpKey("clientId-standard-hedging", string.Empty))).Returns(noPolicy);
        provider.Setup(v => v.GetStrategy<HttpResponseMessage>(new HttpKey("clientId-standard-hedging-endpoint", customKey))).Returns(noPolicy);

        using var client = CreateClientWithHandler();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://key:80/discarded");
        AddResponse(HttpStatusCode.OK);

        var response = await client.SendAsync(request, CancellationToken.None);

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
                { "standard:HedgingOptions:MaxHedgedAttempts", "3" }
            },
            out var reloadAction).GetSection("standard");

        Builder.Configure(config).Configure(options => options.HedgingOptions.HedgingDelay = Timeout.InfiniteTimeSpan);
        SetupRouting();
        SetupRoutes(10);

        var client = CreateClientWithHandler();

        // act && assert
        AddResponse(HttpStatusCode.InternalServerError, 3);
        using var firstRequest = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        await client.SendAsync(firstRequest);
        AssertNoResponse();

        reloadAction(new() { { "standard:HedgingOptions:MaxHedgedAttempts", "7" } });

        AddResponse(HttpStatusCode.InternalServerError, 7);
        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        await client.SendAsync(secondRequest);
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
        await client.SendAsync(firstRequest);
        AssertNoResponse();

        Requests.Should().AllSatisfy(r => r.Should().Be("https://some-endpoint:1234/some-path?query"));
    }

    protected override void ConfigureHedgingOptions(Action<HttpHedgingStrategyOptions> configure) => Builder.Configure(options => configure(options.HedgingOptions));
}
