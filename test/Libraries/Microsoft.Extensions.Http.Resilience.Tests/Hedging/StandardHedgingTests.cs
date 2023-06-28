// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Hedging;
using Polly.Registry;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

public sealed class StandardHedgingTests : HedgingTests<IStandardHedgingHandlerBuilder>
{
    public StandardHedgingTests()
        : base(ConfigureDefaultBuilder)
    {
    }

    private static IStandardHedgingHandlerBuilder ConfigureDefaultBuilder(IHttpClientBuilder builder, IRequestRoutingStrategyFactory factory)
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

        primary.Properties.Set(ResilienceKeys.RequestSnapshot, Mock.Of<IHttpRequestMessageSnapshot>());
        generator.Invoking(g => g(args)).Should().Throw<InvalidOperationException>().WithMessage("Routing strategy is not attached to the resilience context.");
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
    public async Task VerifyPipeline()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        var strategies = new List<IList<ResilienceStrategy>>();

        Builder.Services.RemoveAll<ResilienceStrategyBuilder>();
        Builder.Services.AddTransient(_ =>
        {
            return new ResilienceStrategyBuilder
            {
                OnCreatingStrategy = list => strategies.Add(list)
            };
        });
        Builder.SelectStrategyByAuthority(SimpleClassifications.PublicData);

        SetupRouting();
        SetupRoutes(1);
        SetupCloner(request, false);
        AddResponse(HttpStatusCode.OK);

        using var client = CreateClientWithHandler();
        await client.SendAsync(request, CancellationToken.None);

        // primary handler
        strategies.Should().HaveCount(2);
        strategies[0].Should().HaveCount(4);
        strategies[0][0].GetType().Name.Should().Contain("Routing");
        strategies[0][1].GetType().Name.Should().Contain("Snapshot");
        strategies[0][2].GetType().Name.Should().Contain("Timeout");
        strategies[0][3].GetType().Name.Should().Contain("Hedging");

        // inner handler
        strategies[1].Should().HaveCount(3);
        strategies[1][0].GetType().Name.Should().Contain("RateLimiter");
        strategies[1][1].GetType().Name.Should().Contain("CircuitBreaker");
        strategies[1][2].GetType().Name.Should().Contain("Timeout");
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
        SetupCloner(firstRequest, true);
        await client.SendAsync(firstRequest);
        AssertNoResponse();

        reloadAction(new() { { "standard:HedgingOptions:MaxHedgedAttempts", "7" } });

        AddResponse(HttpStatusCode.InternalServerError, 7);
        using var secondRequest = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        SetupCloner(secondRequest, true);
        await client.SendAsync(secondRequest);
        AssertNoResponse();
    }

    protected override void ConfigureHedgingOptions(Action<HttpHedgingStrategyOptions> configure) => Builder.Configure(options => configure(options.HedgingOptions));
}
