// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ResponseContinuationTokenTests
{
    [Fact]
    public void Bytes_Roundtrip()
    {
        byte[] testBytes = [1, 2, 3, 4, 5];

        ResponseContinuationToken token = ResponseContinuationToken.FromBytes(testBytes);

        Assert.NotNull(token);
        Assert.Equal(testBytes, token.ToBytes());
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ResponseContinuationToken originalToken = ResponseContinuationToken.FromBytes(new byte[] { 1, 2, 3, 4, 5 });

        // Act
        string json = JsonSerializer.Serialize(originalToken, TestJsonSerializerContext.Default.ResponseContinuationToken);

        ResponseContinuationToken? deserializedToken = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ResponseContinuationToken);

        // Assert
        Assert.NotNull(deserializedToken);
        Assert.Equal(originalToken.ToBytes(), deserializedToken.ToBytes());
        Assert.NotSame(originalToken, deserializedToken);
    }
}
