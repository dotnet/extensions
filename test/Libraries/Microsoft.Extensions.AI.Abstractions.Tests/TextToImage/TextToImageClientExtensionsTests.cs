// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class TextToImageClientExtensionsTests
{
    [Fact]
    public void GetService_InvalidArgs_Throws()
    {
        Assert.Throws<ArgumentNullException>("client", () =>
        {
            _ = TextToImageClientExtensions.GetService<object>(null!);
        });
    }

    [Fact]
    public void GetService_ValidClient_CallsUnderlyingGetService()
    {
        using var testClient = new TestTextToImageClient();
        var expectedResult = new object();
        var expectedServiceKey = new object();

        testClient.GetServiceCallback = (serviceType, serviceKey) =>
        {
            Assert.Equal(typeof(object), serviceType);
            Assert.Same(expectedServiceKey, serviceKey);
            return expectedResult;
        };

        var result = testClient.GetService<object>(expectedServiceKey);
        Assert.Same(expectedResult, result);
    }

    [Fact]
    public void GetService_ReturnsCorrectType()
    {
        using var testClient = new TestTextToImageClient();
        var metadata = new TextToImageClientMetadata("test", null, "model");

        testClient.GetServiceCallback = (serviceType, serviceKey) =>
        {
            return (serviceType == typeof(TextToImageClientMetadata)) ? metadata : null;
        };

        var result = testClient.GetService<TextToImageClientMetadata>();
        Assert.Same(metadata, result);

        var nullResult = testClient.GetService<string>();
        Assert.Null(nullResult);
    }
}
