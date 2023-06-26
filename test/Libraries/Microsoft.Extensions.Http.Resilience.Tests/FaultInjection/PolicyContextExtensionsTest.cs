// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Test;

public class PolicyContextExtensionsTest
{
    [Fact]
    public void WithCallingRequestMessage_ShouldSetRequestMessageInContext()
    {
        var context = new Context();
        var method = HttpMethod.Get;
        var testUri = new Uri("https://localhost:12345");
        using var testHttpRequest = new HttpRequestMessage(method, testUri);

        context.WithCallingRequestMessage(testHttpRequest);
        var result = context.GetCallingRequestMessage();
        Assert.Equal(testHttpRequest, result);
    }

    [Fact]
    public void WithCallingRequestMessage_NullContext_ShouldThrow()
    {
        var method = HttpMethod.Get;
        var testUri = new Uri("https://localhost:12345");
        using var testHttpRequest = new HttpRequestMessage(method, testUri);

        Context? context = null!;
        Assert.Throws<ArgumentNullException>(() => context.WithCallingRequestMessage(testHttpRequest));
    }

    [Fact]
    public void WithCallingRequestMessage_NullRequest_ShouldThrow()
    {
        var context = new Context();
        Assert.Throws<ArgumentNullException>(() => context.WithCallingRequestMessage(null!));
    }

    [Fact]
    public void GetCallingRequestMessage_RequestMessageNotSet_ShouldReturnNull()
    {
        var context = new Context();

        var result = context.GetCallingRequestMessage();
        Assert.Null(result);
    }

    [Fact]
    public void GetCallingRequestMessage_RequestMessageSetToIncompatibleObject_ShouldReturnNull()
    {
        var context = new Context
        {
            ["CallingRequestMessage"] = new object()
        };

        var result = context.GetCallingRequestMessage();
        Assert.Null(result);
    }
}
