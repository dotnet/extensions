// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Internal;
using System.Cloud.Messaging.Tests.Data;
using Microsoft.AspNetCore.Http.Features;
using Xunit;

namespace System.Cloud.Messaging.Tests.Extensions;

/// <summary>
/// Tests for <see cref="SerializedMessagePayloadFeatureExtensions"/>.
/// </summary>
public class SerializedMessagePayloadExtensionsTests
{
    [Fact]
    public void GetSerializedMessagePayload_ShouldThrowException_WhenSerializedPayloadIsNotSet()
    {
        var context = new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty);
        var exception = Assert.Throws<InvalidOperationException>(() => context.GetSerializedPayload<string>());
        Assert.Equal(ExceptionMessages.NoSerializedMessagePayloadFeatureOnMessageContext, exception.Message);
    }

    [Fact]
    public void TryGetSerializedMessagePayload_ShouldReturnFalse_WhenSerializedPayloadIsNotSet()
    {
        var context = new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty);
        Assert.False(context.TryGetSerializedPayload<string>(out _));
    }

    [Theory]
    [InlineData("abc")]
    public void GetSerializedMessagePayload_ShouldReturnValue_WhenSerializedPayloadIsSet(string payload)
    {
        var context = new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty);
        context.SetSerializedPayload(payload);
        Assert.Equal(payload, context.GetSerializedPayload<string>());
    }

    [Theory]
    [InlineData("abc")]
    public void TryGetSerializedMessagePayload_ShouldReturnValue_WhenSerializedPayloadIsSet(string payload)
    {
        var context = new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty);
        context.SetSerializedPayload(payload);

        Assert.True(context.TryGetSerializedPayload(out string? value));
        Assert.Equal(payload, value);
    }

    [Theory]
    [InlineData(1, "abc")]
    public void GetSerializedMessagePayload_ShouldReturnValue_WhenSerializedPayloadIsSetForDifferentTypes(int intPayload, string stringPayload)
    {
        var context = new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty);
        context.SetSerializedPayload(intPayload);
        context.SetSerializedPayload(stringPayload);

        Assert.Equal(intPayload, context.GetSerializedPayload<int>());
        Assert.Equal(stringPayload, context.GetSerializedPayload<string>());
    }

    [Theory]
    [InlineData(1, "abc")]
    public void TryGetSerializedMessagePayload_ShouldReturnValue_WhenSerializedPayloadIsSetForDifferentTypes(int intPayload, string stringPayload)
    {
        var context = new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty);
        context.SetSerializedPayload(intPayload);
        context.SetSerializedPayload(stringPayload);

        Assert.True(context.TryGetSerializedPayload(out int intValue));
        Assert.Equal(intPayload, intValue);

        Assert.True(context.TryGetSerializedPayload(out string? stringValue));
        Assert.Equal(stringPayload, stringValue);
    }
}
