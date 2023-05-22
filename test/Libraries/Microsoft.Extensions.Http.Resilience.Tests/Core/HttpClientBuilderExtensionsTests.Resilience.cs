// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Extensions.Telemetry.Metering;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test;

public sealed partial class HttpClientBuilderExtensionsTests
{
    private const string DefaultPolicyName = "dummy-policy-name";

    [Fact]
    public void AddResilienceHandler_ArgumentValidation()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder? builder = services.AddHttpClient("client");

        Assert.Throws<ArgumentNullException>(() => builder.AddResilienceHandler(null!));
        Assert.Throws<ArgumentException>(() => builder.AddResilienceHandler(string.Empty));

        builder = null;
        Assert.Throws<ArgumentNullException>(() => builder!.AddResilienceHandler("pipeline-name"));
    }

    [Fact]
    public void AddResilienceHandler_EnsureOtherPipelineDefaultsNotAffected()
    {
        var called = false;
        var services = new ServiceCollection().RegisterMetering().AddLogging();

        services.ConfigureAll<RetryPolicyOptions<HttpResponseMessage>>(options =>
        {
            Assert.NotEqual(HttpClientResiliencePredicates.IsTransientHttpFailure, options.ShouldHandleResultAsError);
        });

        services
            .AddHttpClient("client")
            .AddResilienceHandler("test").AddRetryPolicy(DefaultPolicyName);

        services
            .AddResiliencePipeline<HttpResponseMessage>("test2")
            .AddRetryPolicy(
                DefaultPolicyName,
                options =>
                {
                    Assert.NotEqual(HttpClientResiliencePredicates.IsTransientHttpFailure, options.ShouldHandleResultAsError);
                    called = true;
                });

        var provider = services.BuildServiceProvider();
        var pipelineProvider = provider.GetRequiredService<IResiliencePipelineProvider>();

        pipelineProvider.GetPipeline<HttpResponseMessage>("client-test");
        pipelineProvider.GetPipeline<HttpResponseMessage>("test2");

        Assert.True(called);
    }

    [Fact]
    public void AddResilienceHandler_EnsureCorrectServicesRegistered()
    {
        var services = new ServiceCollection();
        IHttpClientBuilder? builder = services.AddHttpClient("client");

        builder.AddResilienceHandler("test");

        // add twice intentionally
        builder.AddResilienceHandler("test");

        Assert.Contains(services, s => s.ServiceType == typeof(IPolicyFactory));
    }

    public enum PolicyType
    {
        Fallback,
        Retry,
        CircuitBreaker,
    }

    [InlineData(PolicyType.Fallback)]
    [InlineData(PolicyType.CircuitBreaker)]
    [InlineData(PolicyType.Retry)]
    [Theory]
    public async Task AddResilienceHandler_IndividialPolicies_EnsureProperDelegatesRegistered(PolicyType policyType)
    {
        // arrange
        var called = false;
        var services = new ServiceCollection().AddLogging().RegisterMetering();
        var builder = services.AddHttpClient("client");

        ConfigureAndAssertPolicies(policyType, builder.AddResilienceHandler("test-pipeline"), () => called = true);

        builder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));
        services.ConfigureHttpFailureResultContext();

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("client");
        var pipelineProvider = provider.GetRequiredService<IResiliencePipelineProvider>();

        // act
        await client.GetAsync("https://dummy");

        // assert
        Assert.True(called);

        Assert.NotNull(pipelineProvider.GetPipeline<HttpResponseMessage>("client-test-pipeline"));
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public async Task AddResilienceHandler_EnsureProperPipelineInstanceRetrieved(bool bySelector)
    {
        // arrange
        var resilienceProvider = new Mock<IResiliencePipelineProvider>(MockBehavior.Strict);
        var services = new ServiceCollection().AddLogging().RegisterMetering().AddFakeRedaction();
        services.AddSingleton(resilienceProvider.Object);
        var builder = services.AddHttpClient("client");
        var pipelineBuilder = builder.AddResilienceHandler("dummy");
        var expectedPipelineName = "client-dummy";
        if (bySelector)
        {
            pipelineBuilder.SelectPipelineByAuthority(DataClassification.Unknown);
        }

        pipelineBuilder.AddRetryPolicy("test");
        builder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        var provider = services.BuildServiceProvider();
        if (bySelector)
        {
            resilienceProvider.Setup(v => v.GetPipeline<HttpResponseMessage>(expectedPipelineName, "https://dummy1")).Returns(Policy.NoOpAsync<HttpResponseMessage>());
        }
        else
        {
            resilienceProvider.Setup(v => v.GetPipeline<HttpResponseMessage>(expectedPipelineName)).Returns(Policy.NoOpAsync<HttpResponseMessage>());
        }

        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("client");

        // act
        await client.GetAsync("https://dummy1");

        // assert
        resilienceProvider.VerifyAll();
    }

    [Fact]
    public async Task AddResilienceHandlerBySelector_EnsurePolicyProviderCalled()
    {
        // arrange
        var services = new ServiceCollection().AddLogging().RegisterMetering();
        var providerMock = new Mock<IResiliencePipelineProvider>(MockBehavior.Strict);
        services.AddSingleton(providerMock.Object);
        var pipelineName = string.Empty;

        pipelineName = "client-my-pipeline";
        var clientBuilder = services.AddHttpClient("client");
        clientBuilder
            .AddResilienceHandler("my-pipeline")
            .AddRetryPolicy(DefaultPolicyName);
        clientBuilder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        providerMock
            .Setup(v => v.GetPipeline<HttpResponseMessage>(pipelineName))
            .Returns(Policy.NoOpAsync<HttpResponseMessage>())
            .Verifiable();

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("client");
        var pipelineProvider = provider.GetRequiredService<IResiliencePipelineProvider>();

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
            .AddResilienceHandler("my-pipeline")
            .SelectPipelineByAuthority(SimpleClassifications.PrivateData)
            .AddRetryPolicy(DefaultPolicyName);

        var factory = clientBuilder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient("my-client"));
        Assert.Equal("The redacted pipeline is an empty string and cannot be used for pipeline selection. Is redaction correctly configured?", error.Message);
    }

    [Fact]
    public void AddResilienceHandler_AuthorityByCustomSelector_NotValidated()
    {
        // arrange
        var clientBuilder = new ServiceCollection().AddLogging().RegisterMetering().AddRedaction()
            .AddHttpClient("my-client")
            .AddResilienceHandler("my-pipeline")
            .SelectPipelineBy(_ => _ => string.Empty)
            .AddRetryPolicy(DefaultPolicyName);

        var factory = clientBuilder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        Assert.NotNull(factory.CreateClient("my-client"));
    }

    private static void ConfigureAndAssertPolicies(PolicyType policyType, IResiliencePipelineBuilder<HttpResponseMessage> builder, Action onCalled)
    {
        var optionsName = $"{builder.PipelineName}-{policyType}-{DefaultPolicyName}";

        if (policyType == PolicyType.Fallback)
        {
            builder.AddFallbackPolicy(
                DefaultPolicyName,
                args => Task.FromResult(new HttpResponseMessage()),
                options => onCalled());
        }
        else if (policyType == PolicyType.Retry)
        {
            builder.AddRetryPolicy(DefaultPolicyName, options => onCalled());
        }
        else if (policyType == PolicyType.CircuitBreaker)
        {
            builder.AddCircuitBreakerPolicy(DefaultPolicyName, options => onCalled());
        }
        else
        {
            throw new NotSupportedException();
        }
    }
}
