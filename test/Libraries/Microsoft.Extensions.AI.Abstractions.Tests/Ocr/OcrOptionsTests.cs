// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class OcrOptionsTests
{
    [Fact]
    public void Clone_CopiesAllProperties()
    {
        var options = new OcrOptions
        {
            ModelId = "mistral-ocr-4-0",
            AdditionalProperties = new() { ["custom"] = "value" },
        };

        var clone = options.Clone();

        Assert.NotSame(options, clone);
        Assert.Equal("mistral-ocr-4-0", clone.ModelId);
        Assert.NotNull(clone.AdditionalProperties);
        Assert.Equal("value", clone.AdditionalProperties!["custom"]);
    }

    [Fact]
    public void Clone_DeepCopiesAdditionalProperties()
    {
        var options = new OcrOptions
        {
            AdditionalProperties = new() { ["key"] = "original" },
        };

        var clone = options.Clone();
        clone.AdditionalProperties!["key"] = "changed";

        Assert.Equal("original", options.AdditionalProperties!["key"]);
    }
}
