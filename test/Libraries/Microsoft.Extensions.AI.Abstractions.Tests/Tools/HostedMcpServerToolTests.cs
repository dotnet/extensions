// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedMcpServerToolTests
{
    [Fact]
    public void Constructor_PropsDefault()
    {
        HostedMcpServerTool tool = new("serverName", "https://localhost/");

        Assert.Empty(tool.AdditionalProperties);

        Assert.Equal("serverName", tool.ServerName);
        Assert.Equal("https://localhost/", tool.Url.ToString());

        Assert.Empty(tool.Description);
        Assert.Null(tool.AllowedTools);
        Assert.Null(tool.ApprovalMode);
    }

    [Fact]
    public void Constructor_Roundtrips()
    {
        HostedMcpServerTool tool = new("serverName", "https://localhost/");

        Assert.Empty(tool.AdditionalProperties);
        Assert.Empty(tool.Description);
        Assert.Equal(nameof(HostedMcpServerTool), tool.Name);

        Assert.Equal("serverName", tool.ServerName);
        Assert.Equal("https://localhost/", tool.Url.ToString());
        Assert.Empty(tool.Description);

        Assert.Null(tool.ServerDescription);
        string serverDescription = "This is a test server";
        tool.ServerDescription = serverDescription;
        Assert.Equal(serverDescription, tool.ServerDescription);

        Assert.Null(tool.AllowedTools);
        List<string> allowedTools = ["tool1", "tool2"];
        tool.AllowedTools = allowedTools;
        Assert.Same(allowedTools, tool.AllowedTools);

        Assert.Null(tool.ApprovalMode);
        tool.ApprovalMode = HostedMcpServerToolApprovalMode.NeverRequire;
        Assert.Same(HostedMcpServerToolApprovalMode.NeverRequire, tool.ApprovalMode);

        tool.ApprovalMode = HostedMcpServerToolApprovalMode.AlwaysRequire;
        Assert.Same(HostedMcpServerToolApprovalMode.AlwaysRequire, tool.ApprovalMode);

        var customApprovalMode = new HostedMcpServerToolRequireSpecificApprovalMode(["tool1"], ["tool2"]);
        tool.ApprovalMode = customApprovalMode;
        Assert.Same(customApprovalMode, tool.ApprovalMode);

        Assert.Null(tool.Headers);
        Dictionary<string, string> headers = [];
        tool.Headers = headers;
        Assert.Same(headers, tool.Headers);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>(() => new HostedMcpServerTool(string.Empty, new Uri("https://localhost/")));
        Assert.Throws<ArgumentNullException>(() => new HostedMcpServerTool(null!, new Uri("https://localhost/")));
        Assert.Throws<ArgumentNullException>(() => new HostedMcpServerTool("name", (Uri)null!));
        Assert.Throws<ArgumentNullException>(() => new HostedMcpServerTool("name", (string)null!));
        Assert.Throws<UriFormatException>(() => new HostedMcpServerTool("name", string.Empty));
    }
}
