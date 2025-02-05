// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIFunctionMetadataTests
{
    [Fact]
    public void Constructor_InvalidArg_Throws()
    {
        Assert.Throws<ArgumentNullException>("name", () => new AIFunctionMetadata((string)null!));
        Assert.Throws<ArgumentException>("name", () => new AIFunctionMetadata("  \t  "));
        Assert.Throws<ArgumentNullException>("metadata", () => new AIFunctionMetadata((AIFunctionMetadata)null!));
    }

    [Fact]
    public void Constructor_String_PropsDefaulted()
    {
        AIFunctionMetadata f = new("name");
        Assert.Equal("name", f.Name);
        Assert.Empty(f.Description);
        Assert.Empty(f.Parameters);

        Assert.NotNull(f.ReturnParameter);
        Assert.True(JsonElement.DeepEquals(f.ReturnParameter.Schema, JsonDocument.Parse("{}").RootElement));
        Assert.Null(f.ReturnParameter.ParameterType);
        Assert.Null(f.ReturnParameter.Description);

        Assert.NotNull(f.AdditionalProperties);
        Assert.Empty(f.AdditionalProperties);
        Assert.Same(f.AdditionalProperties, new AIFunctionMetadata("name2").AdditionalProperties);
    }

    [Fact]
    public void Constructor_Copy_PropsPropagated()
    {
        AIFunctionMetadata f1 = new("name")
        {
            Description = "description",
            Parameters = [new AIFunctionParameterMetadata("param")],
            ReturnParameter = new AIFunctionReturnParameterMetadata(),
            AdditionalProperties = new Dictionary<string, object?> { { "key", "value" } },
        };

        AIFunctionMetadata f2 = new(f1);
        Assert.Equal(f1.Name, f2.Name);
        Assert.Equal(f1.Description, f2.Description);
        Assert.Same(f1.Parameters, f2.Parameters);
        Assert.Same(f1.ReturnParameter, f2.ReturnParameter);
        Assert.Same(f1.AdditionalProperties, f2.AdditionalProperties);
    }

    [Fact]
    public void Props_InvalidArg_Throws()
    {
        Assert.Throws<ArgumentNullException>("value", () => new AIFunctionMetadata("name") { Name = null! });
        Assert.Throws<ArgumentNullException>("value", () => new AIFunctionMetadata("name") { Parameters = null! });
        Assert.Throws<ArgumentNullException>("value", () => new AIFunctionMetadata("name") { ReturnParameter = null! });
        Assert.Throws<ArgumentNullException>("value", () => new AIFunctionMetadata("name") { AdditionalProperties = null! });
    }

    [Fact]
    public void Description_NullNormalizedToEmpty()
    {
        AIFunctionMetadata f = new("name") { Description = null };
        Assert.Equal("", f.Description);
    }

    [Fact]
    public void GetParameter_EmptyCollection_ReturnsNull()
    {
        Assert.Null(new AIFunctionMetadata("name").GetParameter("test"));
    }

    [Fact]
    public void GetParameter_ByName_ReturnsParameter()
    {
        AIFunctionMetadata f = new("name")
        {
            Parameters =
            [
                new AIFunctionParameterMetadata("param0"),
                new AIFunctionParameterMetadata("param1"),
                new AIFunctionParameterMetadata("param2"),
            ]
        };

        Assert.Same(f.Parameters[0], f.GetParameter("param0"));
        Assert.Same(f.Parameters[1], f.GetParameter("param1"));
        Assert.Same(f.Parameters[2], f.GetParameter("param2"));
        Assert.Null(f.GetParameter("param3"));
    }
}
