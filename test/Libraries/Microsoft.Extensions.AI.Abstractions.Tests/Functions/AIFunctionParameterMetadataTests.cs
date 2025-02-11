// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIFunctionParameterMetadataTests
{
    [Fact]
    public void Constructor_InvalidArg_Throws()
    {
        Assert.Throws<ArgumentNullException>("name", () => new AIFunctionParameterMetadata((string)null!));
        Assert.Throws<ArgumentException>("name", () => new AIFunctionParameterMetadata("     "));
        Assert.Throws<ArgumentNullException>("metadata", () => new AIFunctionParameterMetadata((AIFunctionParameterMetadata)null!));
    }

    [Fact]
    public void Constructor_String_PropsDefaulted()
    {
        AIFunctionParameterMetadata p = new("name");
        Assert.Equal("name", p.Name);
        Assert.Null(p.Description);
        Assert.Null(p.DefaultValue);
        Assert.False(p.IsRequired);
        Assert.Null(p.ParameterType);
    }

    [Fact]
    public void Constructor_Copy_PropsPropagated()
    {
        AIFunctionParameterMetadata p1 = new("name")
        {
            Description = "description",
            HasDefaultValue = true,
            DefaultValue = 42,
            IsRequired = true,
            ParameterType = typeof(int),
        };

        AIFunctionParameterMetadata p2 = new(p1);

        Assert.Equal(p1.Name, p2.Name);
        Assert.Equal(p1.Description, p2.Description);
        Assert.Equal(p1.DefaultValue, p2.DefaultValue);
        Assert.Equal(p1.IsRequired, p2.IsRequired);
        Assert.Equal(p1.ParameterType, p2.ParameterType);
    }

    [Fact]
    public void Constructor_Copy_PropsPropagatedAndOverwritten()
    {
        AIFunctionParameterMetadata p1 = new("name")
        {
            Description = "description",
            HasDefaultValue = true,
            DefaultValue = 42,
            IsRequired = true,
            ParameterType = typeof(int),
        };

        AIFunctionParameterMetadata p2 = new(p1)
        {
            Description = "description2",
            HasDefaultValue = true,
            DefaultValue = 43,
            IsRequired = false,
            ParameterType = typeof(long),
        };

        Assert.Equal("description2", p2.Description);
        Assert.True(p2.HasDefaultValue);
        Assert.Equal(43, p2.DefaultValue);
        Assert.False(p2.IsRequired);
        Assert.Equal(typeof(long), p2.ParameterType);
    }

    [Fact]
    public void Props_InvalidArg_Throws()
    {
        Assert.Throws<ArgumentNullException>("value", () => new AIFunctionMetadata("name") { Name = null! });
        Assert.Throws<ArgumentException>("value", () => new AIFunctionMetadata("name") { Name = "\r\n\t " });
    }
}
