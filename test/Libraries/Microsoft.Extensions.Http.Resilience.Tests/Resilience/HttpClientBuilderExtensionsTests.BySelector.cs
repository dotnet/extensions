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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Resilience.Test.Helpers;
using Moq;
using Polly;
using Polly.Registry;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test;

public sealed partial class HttpClientBuilderExtensionsTests
{
    [InlineData(true, "https://dummy:21/path", "https://dummy:21")]
    [InlineData(true, "https://dummy", "https://dummy")]
    [InlineData(false, "https://dummy:21/path", "https://dummy:21")]
    [InlineData(false, "https://dummy", "https://dummy")]
    [Theory]
    public void SelectStrategyByAuthority_Ok(bool standardResilience, string url, string expectedStrategyKey)
    {
        _builder.Services.AddFakeRedaction();

        var pipelineName = standardResilience ?
            _builder.AddStandardResilienceHandler().SelectStrategyByAuthority(DataClassification.Unknown).StrategyName :
            _builder
                .AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1)))
                .SelectStrategyByAuthority(DataClassification.Unknown).StrategyName;

        var provider = _builder.Services.BuildServiceProvider().GetStrategyKeyProvider(pipelineName)!;

        using var request = new HttpRequestMessage(HttpMethod.Head, url);

        var key = provider.GetStrategyKey(request);

        Assert.Equal(expectedStrategyKey, key);
        Assert.Same(provider.GetStrategyKey(request), provider.GetStrategyKey(request));
    }

    [Fact]
    public void SelectStrategyByAuthority_Ok_NullURL_Throws()
    {
        _builder.Services.AddFakeRedaction();
        var builder = _builder.AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1))).SelectStrategyByAuthority(DataClassification.Unknown);
        var provider = StrategyKeyProviderHelper.GetStrategyKeyProvider(builder.Services.BuildServiceProvider(), builder.StrategyName)!;

        using var request = new HttpRequestMessage();

        Assert.Throws<InvalidOperationException>(() => provider.GetStrategyKey(request));
    }

    [Fact]
    public void SelectStrategyByAuthority_ErasingRedactor_InvalidOperationException()
    {
        _builder.Services.AddRedaction();
        var builder = _builder.AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1))).SelectStrategyByAuthority(SimpleClassifications.PrivateData);
        var provider = StrategyKeyProviderHelper.GetStrategyKeyProvider(builder.Services.BuildServiceProvider(), builder.StrategyName)!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://dummy");

        Assert.Throws<InvalidOperationException>(() => provider.GetStrategyKey(request));
    }

    [InlineData(true, "https://dummy:21/path", "https://")]
    [InlineData(true, "https://dummy", "https://")]
    [InlineData(false, "https://dummy:21/path", "https://")]
    [InlineData(false, "https://dummy", "https://")]
    [Theory]
    public void SelectStrategyBy_Ok(bool standardResilience, string url, string expectedStrategyKey)
    {
        _builder.Services.AddFakeRedaction();

        string? pipelineName = null;

        if (standardResilience)
        {
            pipelineName = _builder
                .AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1)))
                .SelectStrategyBy(_ => r => r.RequestUri!.GetLeftPart(UriPartial.Scheme)).StrategyName;
        }
        else
        {
            pipelineName = _builder
                .AddStandardResilienceHandler()
                .SelectStrategyBy(_ => r => r.RequestUri!.GetLeftPart(UriPartial.Scheme)).StrategyName;
        }

        var provider = _builder.Services.BuildServiceProvider().GetStrategyKeyProvider(pipelineName)!;

        using var request = new HttpRequestMessage(HttpMethod.Head, url);

        var key = provider.GetStrategyKey(request);

        Assert.Equal(expectedStrategyKey, key);
        Assert.NotSame(provider.GetStrategyKey(request), provider.GetStrategyKey(request));
    }

    [InlineData(true, "https://dummy:21/path", "https://dummy:21")]
    [InlineData(true, "https://dummy123", "https://dummy123")]
    [InlineData(false, "https://dummy:21/path", "https://dummy:21")]
    [InlineData(false, "https://dummy123", "https://dummy123")]
    [Theory]
    public async Task SelectStrategyBy_EnsureResilienceStrategyProviderCall(bool standardResilience, string url, string expectedStrategyKey)
    {
        var strategyProvider = new Mock<ResilienceStrategyProvider<HttpKey>>(MockBehavior.Strict);

        _builder.Services.AddFakeRedaction();
        _builder.Services.TryAddSingleton(strategyProvider.Object);
        string? pipelineName = null;
        if (standardResilience)
        {
            pipelineName = _builder
                .AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1)))
                .SelectStrategyByAuthority(DataClassification.None).StrategyName;
        }
        else
        {
            pipelineName = _builder
                .AddStandardResilienceHandler()
                .SelectStrategyByAuthority(DataClassification.None).StrategyName;
        }

        _builder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        strategyProvider
            .Setup(p => p.GetStrategy<HttpResponseMessage>(new HttpKey(pipelineName, expectedStrategyKey)))
            .Returns(NullResilienceStrategy<HttpResponseMessage>.Instance);

        await CreateClient().GetAsync(url);

        strategyProvider.VerifyAll();
    }
}
