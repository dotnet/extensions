// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
        Assert.Null(f.UnderlyingMethod);

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
            UnderlyingMethod = typeof(AIFunctionMetadataTests).GetMethod(nameof(Constructor_Copy_PropsPropagated))!,
            AdditionalProperties = new Dictionary<string, object?> { { "key", "value" } },
        };

        AIFunctionMetadata f2 = new(f1);
        Assert.Equal(f1.Name, f2.Name);
        Assert.Equal(f1.Description, f2.Description);
        Assert.Same(f1.UnderlyingMethod, f2.UnderlyingMethod);
        Assert.Same(f1.AdditionalProperties, f2.AdditionalProperties);
    }

    [Fact]
    public void Props_InvalidArg_Throws()
    {
        Assert.Throws<ArgumentNullException>("value", () => new AIFunctionMetadata("name") { Name = null! });
        Assert.Throws<ArgumentNullException>("value", () => new AIFunctionMetadata("name") { AdditionalProperties = null! });
    }

    [Fact]
    public void Description_NullNormalizedToEmpty()
    {
        AIFunctionMetadata f = new("name") { Description = null };
        Assert.Equal("", f.Description);
    }
}
