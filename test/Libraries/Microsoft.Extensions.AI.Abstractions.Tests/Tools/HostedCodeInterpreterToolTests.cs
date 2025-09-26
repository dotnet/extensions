// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedCodeInterpreterToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        var tool = new HostedCodeInterpreterTool();
        Assert.Equal(nameof(HostedCodeInterpreterTool), tool.Name);
        Assert.Empty(tool.Description);
        Assert.Empty(tool.AdditionalProperties);
        Assert.Null(tool.Inputs);
        Assert.Equal(nameof(HostedCodeInterpreterTool), tool.ToString());
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        var tool = new HostedCodeInterpreterTool
        {
            Inputs =
            [
                new HostedFileContent("id123"),
                new DataContent(new byte[] { 1, 2, 3 }, "application/octet-stream")
            ]
        };

        Assert.NotNull(tool.Inputs);
        Assert.Equal(2, tool.Inputs.Count);
        Assert.IsType<HostedFileContent>(tool.Inputs[0]);
        Assert.IsType<DataContent>(tool.Inputs[1]);
    }
}
