// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Telemetry;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internals;

public class ContextExtensionsTest
{
    [Fact]
    public void ArgumentValidation_Ok()
    {
        Assert.Throws<ArgumentNullException>(() => Resilience.Internal.ContextExtensions.SetRequestMetadata(null!, new HttpRequestMessage()));
        Assert.Throws<ArgumentNullException>(() => Resilience.Internal.ContextExtensions.SetRequestMetadata(new Context(), null!));
    }

    [InlineData("A", null, "A")]
    [InlineData("A", "B", "B")]
    [InlineData(null, "B", "B")]
    [InlineData(null, null, null)]
    [Theory]
    public void SetRequestMetadata_EnsureCorrectBehavior(string? requestMetadata, string? contextMetadata, string? expectedMetadata)
    {
        var context = new Context();
        using var request = new HttpRequestMessage();

        if (requestMetadata != null)
        {
            request.SetRequestMetadata(new RequestMetadata { DependencyName = requestMetadata });
        }

        if (contextMetadata != null)
        {
            context[TelemetryConstants.RequestMetadataKey] = new RequestMetadata { DependencyName = contextMetadata };
        }

        context.SetRequestMetadata(request);

        context.TryGetValue(TelemetryConstants.RequestMetadataKey, out var val);

        Assert.Equal(expectedMetadata, (val as RequestMetadata)?.DependencyName);
    }

    [Fact]
    public void RequestMessageProviderAndSetter_EnsureCorrectBehavior()
    {
        using var message = new HttpRequestMessage();
        var context = new Context();
        var setter = Resilience.Internal.ContextExtensions.CreateRequestMessageSetter("my-pipeline");
        var provider = Resilience.Internal.ContextExtensions.CreateRequestMessageProvider("my-pipeline");
        var providerOther = Resilience.Internal.ContextExtensions.CreateRequestMessageProvider("my-pipeline-other");

        setter(context, message);

        Assert.Equal(message, provider(context));
        Assert.NotEqual(message, providerOther(context));
        Assert.Null(providerOther(context));
    }

    [Fact]
    public void InvokerProviderAndSetter_EnsureCorrectBehavior()
    {
        using var handler = new TestHandlerStub(HttpStatusCode.OK);
        using var invoker = new HttpMessageInvoker(handler);
        var context = new Context();
        var setter = Resilience.Internal.ContextExtensions.CreateMessageInvokerSetter("my-pipeline");
        var provider = Resilience.Internal.ContextExtensions.CreateMessageInvokerProvider("my-pipeline");
        var providerOther = Resilience.Internal.ContextExtensions.CreateMessageInvokerProvider("my-pipeline-other");

        setter(context, new Lazy<HttpMessageInvoker>(() => invoker));

        Assert.Equal(invoker, provider(context));
        Assert.NotEqual(invoker, providerOther(context));
        Assert.Null(providerOther(context));
    }
}
