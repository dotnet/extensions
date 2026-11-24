// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class DecisionOptionsTests
{
    [Fact]
    public void Options_CloneCopiesValues()
    {
        Func<IDecisionClient, object?> factory = static _ => new object();
        DecisionOptions options = new()
        {
            ModelId = "model",
            AdditionalProperties = new() { ["key"] = "value" },
            RawRepresentationFactory = factory,
        };

        DecisionOptions clone = options.Clone();

        Assert.Equal("model", clone.ModelId);
        Assert.NotSame(options.AdditionalProperties, clone.AdditionalProperties);
        Assert.Equal("value", clone.AdditionalProperties!["key"]);
        Assert.Same(factory, clone.RawRepresentationFactory);
    }
}