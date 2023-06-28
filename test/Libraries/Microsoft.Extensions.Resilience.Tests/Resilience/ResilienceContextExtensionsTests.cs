// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Resilience.Resilience;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test.Resilience;

public class ResilienceContextExtensionsTests
{
    [Fact]
    public void GetRequestMetadata_Ok()
    {
        var context = ResilienceContext.Get();

        context.GetRequestMetadata().Should().BeNull();
    }

    [Fact]
    public void SetRequestMetadata_Ok()
    {
        var context = ResilienceContext.Get();
        var metadata = new RequestMetadata();

        context.SetRequestMetadata(metadata);

        context.GetRequestMetadata().Should().BeSameAs(metadata);
    }
}
