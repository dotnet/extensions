// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Classification;
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
    public void SelectPipelineByAuthority_Ok(bool standardResilience, string url, string expectedPipelineKey)
    {
        _builder.Services.AddFakeRedaction();

        var pipelineName = standardResilience ?
            _builder.AddStandardResilienceHandler().SelectPipelineByAuthority(DataClassification.Unknown).PipelineName :
            _builder
                .AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1)))
                .SelectPipelineByAuthority(DataClassification.Unknown).PipelineName;

        var provider = _builder.Services.BuildServiceProvider().GetPipelineKeyProvider(pipelineName)!;

        using var request = new HttpRequestMessage(HttpMethod.Head, url);

        var key = provider(request);

        Assert.Equal(expectedPipelineKey, key);
        Assert.Same(provider(request), provider(request));
    }

    [Fact]
    public void SelectPipelineByAuthority_Ok_NullURL_Throws()
    {
        _builder.Services.AddFakeRedaction();
        var builder = _builder.AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1))).SelectPipelineByAuthority(DataClassification.Unknown);
        var provider = PipelineKeyProviderHelper.GetPipelineKeyProvider(builder.Services.BuildServiceProvider(), builder.PipelineName)!;

        using var request = new HttpRequestMessage();

        Assert.Throws<InvalidOperationException>(() => provider(request));
    }

    [Fact]
    public void SelectPipelineByAuthority_ErasingRedactor_InvalidOperationException()
    {
        _builder.Services.AddRedaction();
        var builder = _builder.AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1))).SelectPipelineByAuthority(FakeClassifications.PrivateData);
        var provider = PipelineKeyProviderHelper.GetPipelineKeyProvider(builder.Services.BuildServiceProvider(), builder.PipelineName)!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://dummy");

        Assert.Throws<InvalidOperationException>(() => provider(request));
    }

    [InlineData(true, "https://dummy:21/path", "https://")]
    [InlineData(true, "https://dummy", "https://")]
    [InlineData(false, "https://dummy:21/path", "https://")]
    [InlineData(false, "https://dummy", "https://")]
    [Theory]
    public void SelectPipelineBy_Ok(bool standardResilience, string url, string expectedPipelineKey)
    {
        _builder.Services.AddFakeRedaction();

        string? pipelineName = null;

        if (standardResilience)
        {
            pipelineName = _builder
                .AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1)))
                .SelectPipelineBy(_ => r => r.RequestUri!.GetLeftPart(UriPartial.Scheme)).PipelineName;
        }
        else
        {
            pipelineName = _builder
                .AddStandardResilienceHandler()
                .SelectPipelineBy(_ => r => r.RequestUri!.GetLeftPart(UriPartial.Scheme)).PipelineName;
        }

        var provider = _builder.Services.BuildServiceProvider().GetPipelineKeyProvider(pipelineName)!;

        using var request = new HttpRequestMessage(HttpMethod.Head, url);

        var key = provider(request);

        Assert.Equal(expectedPipelineKey, key);
        Assert.NotSame(provider(request), provider(request));
    }

    [InlineData(true, "https://dummy:21/path", "https://dummy:21")]
    [InlineData(true, "https://dummy123", "https://dummy123")]
    [InlineData(false, "https://dummy:21/path", "https://dummy:21")]
    [InlineData(false, "https://dummy123", "https://dummy123")]
    [Theory]
    public async Task SelectPipelineBy_EnsureResiliencePipelineProviderCall(bool standardResilience, string url, string expectedPipelineKey)
    {
        var provider = new Mock<ResiliencePipelineProvider<HttpKey>>(MockBehavior.Strict);

        _builder.Services.AddFakeRedaction();
        _builder.Services.TryAddSingleton(provider.Object);
        string? pipelineName = null;
        if (standardResilience)
        {
            pipelineName = _builder
                .AddResilienceHandler("dummy", builder => builder.AddTimeout(TimeSpan.FromSeconds(1)))
                .SelectPipelineByAuthority(DataClassification.None).PipelineName;
        }
        else
        {
            pipelineName = _builder
                .AddStandardResilienceHandler()
                .SelectPipelineByAuthority(DataClassification.None).PipelineName;
        }

        _builder.AddHttpMessageHandler(() => new TestHandlerStub(HttpStatusCode.OK));

        provider
            .Setup(p => p.GetPipeline<HttpResponseMessage>(new HttpKey(pipelineName, expectedPipelineKey)))
            .Returns(ResiliencePipeline<HttpResponseMessage>.Empty);

        await CreateClient().GetAsync(url);

        provider.VerifyAll();
    }
}
