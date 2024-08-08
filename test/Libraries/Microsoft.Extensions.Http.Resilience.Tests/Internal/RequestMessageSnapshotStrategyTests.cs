// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internal;

public class RequestMessageSnapshotStrategyTests
{
    [Fact]
    public async Task ExecuteAsync_EnsureSnapshotAttached()
    {
        var strategy = Create();
        var context = ResilienceContextPool.Shared.Get();
        using var request = new HttpRequestMessage();
        context.Properties.Set(ResilienceKeys.RequestMessage, request);

        using var response = await strategy.ExecuteAsync(
            context =>
            {
                context.Properties.GetValue(ResilienceKeys.RequestSnapshot, null!).Should().NotBeNull();
                return new ValueTask<HttpResponseMessage>(new HttpResponseMessage());
            },
            context);
    }

    [Fact]
    public void ExecuteAsync_RequestMessageNotFound_Throws()
    {
        var strategy = Create();

        strategy.Invoking(s => s.Execute(() => { })).Should().Throw<InvalidOperationException>();
    }

    private static ResiliencePipeline Create() => new ResiliencePipelineBuilder().AddStrategy(_ => new RequestMessageSnapshotStrategy(), Mock.Of<ResilienceStrategyOptions>()).Build();
}
