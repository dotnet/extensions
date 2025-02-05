// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIFunctionReturnParameterMetadataTests
{
    [Fact]
    public void Constructor_PropsDefaulted()
    {
        AIFunctionReturnParameterMetadata p = new();
        Assert.Null(p.Description);
        Assert.Null(p.ParameterType);
        Assert.True(JsonElement.DeepEquals(p.Schema, JsonDocument.Parse("{}").RootElement));
    }

    [Fact]
    public void Constructor_Copy_PropsPropagated()
    {
        AIFunctionReturnParameterMetadata p1 = new()
        {
            Description = "description",
            ParameterType = typeof(int),
            Schema = JsonDocument.Parse("""{"type":"integer"}""").RootElement,
        };

        AIFunctionReturnParameterMetadata p2 = new(p1);
        Assert.Equal(p1.Description, p2.Description);
        Assert.Equal(p1.ParameterType, p2.ParameterType);
        Assert.Equal(p1.Schema, p2.Schema);
    }
}
