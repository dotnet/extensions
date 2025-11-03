// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileSearchToolTests
{
    [Fact]
    public void Constructor_Roundtrips()
    {
        var tool = new HostedFileSearchTool();
        Assert.Equal("file_search", tool.Name);
        Assert.Empty(tool.Description);
        Assert.Empty(tool.AdditionalProperties);
        Assert.Null(tool.Inputs);
        Assert.Null(tool.MaximumResultCount);
        Assert.Equal(tool.Name, tool.ToString());
    }

    [Fact]
    public void Properties_Roundtrip()
    {
        var tool = new HostedFileSearchTool
        {
            Inputs =
            [
                new HostedVectorStoreContent("id123"),
                new HostedFileContent("id456"),
            ],
            MaximumResultCount = 10,
        };

        Assert.NotNull(tool.Inputs);
        Assert.Equal(2, tool.Inputs.Count);
        Assert.Equal(10, tool.MaximumResultCount);
        Assert.IsType<HostedVectorStoreContent>(tool.Inputs[0]);
        Assert.IsType<HostedFileContent>(tool.Inputs[1]);
    }
}
