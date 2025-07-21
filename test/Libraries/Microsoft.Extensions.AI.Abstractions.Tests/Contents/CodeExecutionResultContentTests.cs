// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class CodeExecutionResultContentTests
{
    [Fact]
    public void Constructor_String_PropsDefault()
    {
        CodeExecutionResultContent c = new();
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Null(c.Inputs);
        Assert.Null(c.Outputs);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        CodeExecutionResultContent c = new();

        Assert.Null(c.Inputs);
        List<AIContent> inputs = [new TextContent("input1")];
        c.Inputs = inputs;
        Assert.Same(inputs, c.Inputs);

        Assert.Null(c.Outputs);
        List<AIContent> outputs = [new TextContent("output1")];
        c.Outputs = outputs;
        Assert.Same(outputs, c.Outputs);

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);
    }
}
