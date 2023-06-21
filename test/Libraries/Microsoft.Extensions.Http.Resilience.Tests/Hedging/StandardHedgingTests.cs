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
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Hedgings.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Internal;
using Moq;
using Polly;
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
        Builder.Configure(options => options.TotalRequestTimeoutOptions.TimeoutInterval = TimeSpan.FromSeconds(1));

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
            serviceProvider.GetRequiredService<IResiliencePipelineProvider>().Should().NotBeNull();
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
        var noPolicy = Policy.NoOpAsync<HttpResponseMessage>();
        var builder = new Mock<IPolicyPipelineBuilder<HttpResponseMessage>>(MockBehavior.Strict);
        Builder.Services.RemoveAll<IPolicyPipelineBuilder<HttpResponseMessage>>();
        Builder.Services.AddSingleton(builder.Object);

        var serviceProvider = Builder.Services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<HttpStandardHedgingResilienceOptions>>().Get(Builder.Name);

        // primary handler
        builder.SetupSequence(o => o.Initialize(It.Is<PipelineId>(v => v.PipelineName == "clientId-standard-hedging")));
        builder.SetupSequence(o => o.AddTimeoutPolicy("StandardHedging-TotalRequestTimeout", options.TotalRequestTimeoutOptions)).Returns(builder.Object);
        builder.SetupSequence(o => o.AddPolicy(It.Is<IAsyncPolicy<HttpResponseMessage>>(p => p is RoutingPolicy))).Returns(builder.Object);
        builder.SetupSequence(o => o.AddPolicy(It.Is<IAsyncPolicy<HttpResponseMessage>>(p => p is RequestMessageSnapshotPolicy))).Returns(builder.Object);
        builder.SetupSequence(o => o.AddHedgingPolicy("StandardHedging-Hedging", It.IsAny<HedgedTaskProvider<HttpResponseMessage>>(), options.HedgingOptions)).Returns(builder.Object);
        builder.Setup(o => o.Build()).Returns(noPolicy);

        // inner handler
        builder.SetupSequence(o => o.Initialize(It.Is<PipelineId>(v => v.PipelineName == "clientId-standard-hedging-endpoint")));
        builder.SetupSequence(o => o.AddBulkheadPolicy("StandardHedging-Bulkhead", options.EndpointOptions.BulkheadOptions)).Returns(builder.Object);
        builder.SetupSequence(o => o.AddCircuitBreakerPolicy("StandardHedging-CircuitBreaker", options.EndpointOptions.CircuitBreakerOptions)).Returns(builder.Object);
        builder.SetupSequence(o => o.AddTimeoutPolicy("StandardHedging-AttemptTimeout", options.EndpointOptions.TimeoutOptions)).Returns(builder.Object);
        builder.Setup(o => o.Build()).Returns(noPolicy);

        using var client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(ClientId);
        AddResponse(HttpStatusCode.OK);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://to-be-replaced:1234/some-path?query");
        await client.SendAsync(request, CancellationToken.None);

        builder.VerifyAll();
    }

    [InlineData(null)]
    [InlineData("custom-key")]
    [Theory]
    public async Task VerifyPipelineSelection(string? customKey)
    {
        var noPolicy = Policy.NoOpAsync<HttpResponseMessage>();
        var provider = new Mock<IResiliencePipelineProvider>(MockBehavior.Strict);
        Builder.Services.RemoveAll<IResiliencePipelineProvider>();
        Builder.Services.AddSingleton(provider.Object);
        if (customKey == null)
        {
            Builder.SelectPipelineByAuthority(SimpleClassifications.PublicData);
        }
        else
        {
            Builder.SelectPipelineBy(_ => _ => customKey);
        }

        customKey ??= "https://key:80";
        provider.Setup(v => v.GetPipeline<HttpResponseMessage>("clientId-standard-hedging")).Returns(noPolicy);
        provider.Setup(v => v.GetPipeline<HttpResponseMessage>("clientId-standard-hedging-endpoint", customKey)).Returns(noPolicy);

        using var client = CreateClientWithHandler();
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://key:80/discarded");
        AddResponse(HttpStatusCode.OK);

        var response = await client.SendAsync(request, CancellationToken.None);

        provider.VerifyAll();
    }

    protected override void ConfigureHedgingOptions(Action<HttpHedgingPolicyOptions> configure) => Builder.Configure(options => configure(options.HedgingOptions));
}
