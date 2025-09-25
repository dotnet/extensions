// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class ResumptionTokenTests
{
    [Fact]
    public void Bytes_Roundtrip()
    {
        byte[] testBytes = [1, 2, 3, 4, 5];

        ResumptionToken token = ResumptionToken.FromBytes(testBytes);

        Assert.NotNull(token);
        Assert.Equal(testBytes, token.ToBytes());
    }

    [Fact]
    public void JsonSerialization_Roundtrips()
    {
        ResumptionToken originalToken = ResumptionToken.FromBytes([1, 2, 3, 4, 5]);

        // Act
        string json = JsonSerializer.Serialize(originalToken, TestJsonSerializerContext.Default.ResumptionToken);

        ResumptionToken? deserializedToken = JsonSerializer.Deserialize(json, TestJsonSerializerContext.Default.ResumptionToken);

        // Assert
        Assert.NotNull(deserializedToken);
        Assert.Equal(originalToken.ToBytes(), deserializedToken.ToBytes());
        Assert.NotSame(originalToken, deserializedToken);
    }
}
