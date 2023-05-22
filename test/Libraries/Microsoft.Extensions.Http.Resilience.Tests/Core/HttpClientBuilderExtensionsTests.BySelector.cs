// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience;
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
            _builder.AddResilienceHandler("dummy").SelectPipelineByAuthority(DataClassification.Unknown).AddRetryPolicy("test").PipelineName;

        var provider = _builder.Services.BuildServiceProvider().GetPipelineKeyProvider(pipelineName)!;

        using var request = new HttpRequestMessage(HttpMethod.Head, url);

        var key = provider.GetPipelineKey(request);

        Assert.Equal(expectedPipelineKey, key);
        Assert.Same(provider.GetPipelineKey(request), provider.GetPipelineKey(request));
    }

    [Fact]
    public void SelectPipelineByAuthority_Ok_NullURL_Throws()
    {
        _builder.Services.AddFakeRedaction();
        var builder = _builder.AddResilienceHandler("dummy").SelectPipelineByAuthority(DataClassification.Unknown).AddRetryPolicy("test");
        var provider = PipelineKeyProviderHelper.GetPipelineKeyProvider(builder.Services.BuildServiceProvider(), builder.PipelineName)!;

        using var request = new HttpRequestMessage();

        Assert.Throws<InvalidOperationException>(() => provider.GetPipelineKey(request));
    }

    [Fact]
    public void SelectPipelineByAuthority_ErasingRedactor_InvalidOperationException()
    {
        _builder.Services.AddRedaction();
        var builder = _builder.AddResilienceHandler("dummy").SelectPipelineByAuthority(SimpleClassifications.PrivateData).AddRetryPolicy("test");
        var provider = PipelineKeyProviderHelper.GetPipelineKeyProvider(builder.Services.BuildServiceProvider(), builder.PipelineName)!;

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://dummy");

        Assert.Throws<InvalidOperationException>(() => provider.GetPipelineKey(request));
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
                .AddResilienceHandler("dummy")
                .SelectPipelineBy(_ => r => r.RequestUri!.GetLeftPart(UriPartial.Scheme))
                .AddRetryPolicy("test").PipelineName;
        }
        else
        {
            pipelineName = _builder
                .AddStandardResilienceHandler()
                .SelectPipelineBy(_ => r => r.RequestUri!.GetLeftPart(UriPartial.Scheme)).PipelineName;
        }

        var provider = _builder.Services.BuildServiceProvider().GetPipelineKeyProvider(pipelineName)!;

        using var request = new HttpRequestMessage(HttpMethod.Head, url);

        var key = provider.GetPipelineKey(request);

        Assert.Equal(expectedPipelineKey, key);
        Assert.NotSame(provider.GetPipelineKey(request), provider.GetPipelineKey(request));
    }
}
