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
        HostedMcpServerTool tool = new("serverName", new Uri("https://localhost/"));

        Assert.Empty(tool.AdditionalProperties);

        Assert.Equal("serverName", tool.ServerName);
        Assert.Equal("https://localhost/", tool.ServerAddress);

        Assert.Empty(tool.Description);
        Assert.Null(tool.AuthorizationToken);
        Assert.Null(tool.ServerDescription);
        Assert.Null(tool.AllowedTools);
        Assert.Null(tool.ApprovalMode);
    }

    [Fact]
    public void Constructor_AdditionalProperties_String_Roundtrips()
    {
        var props = new Dictionary<string, object?> { ["key"] = "value" };
        HostedMcpServerTool tool = new("serverName", "connector_id", props);

        Assert.Equal("serverName", tool.ServerName);
        Assert.Equal("connector_id", tool.ServerAddress);
        Assert.Same(props, tool.AdditionalProperties);
    }

    [Fact]
    public void Constructor_AdditionalProperties_Uri_Roundtrips()
    {
        var props = new Dictionary<string, object?> { ["key"] = "value" };
        HostedMcpServerTool tool = new("serverName", new Uri("https://localhost/"), props);

        Assert.Equal("serverName", tool.ServerName);
        Assert.Equal("https://localhost/", tool.ServerAddress);
        Assert.Same(props, tool.AdditionalProperties);
    }

    [Fact]
    public void Constructor_NullAdditionalProperties_UsesEmpty()
    {
        HostedMcpServerTool tool = new("serverName", "connector_id", null);

        Assert.Empty(tool.AdditionalProperties);
    }

    [Fact]
    public void Constructor_Roundtrips()
    {
        HostedMcpServerTool tool = new("serverName", "connector_id");

        Assert.Empty(tool.AdditionalProperties);
        Assert.Empty(tool.Description);
        Assert.Equal("mcp", tool.Name);
        Assert.Equal(tool.Name, tool.ToString());

        Assert.Equal("serverName", tool.ServerName);
        Assert.Equal("connector_id", tool.ServerAddress);
        Assert.Empty(tool.Description);

        Assert.Null(tool.AuthorizationToken);
        string authToken = "Bearer token123";
        tool.AuthorizationToken = authToken;
        Assert.Equal(authToken, tool.AuthorizationToken);

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
        IDictionary<string, string> headers = new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "value1",
        };
        tool.Headers = headers;
        Assert.Same(headers, tool.Headers);

        tool.Headers = null;
        Assert.Null(tool.Headers);
    }

    [Fact]
    public void Constructor_Throws()
    {
        Assert.Throws<ArgumentException>("serverName", () => new HostedMcpServerTool(string.Empty, "https://localhost/"));
        Assert.Throws<ArgumentException>("serverName", () => new HostedMcpServerTool(string.Empty, new Uri("https://localhost/")));
        Assert.Throws<ArgumentNullException>("serverName", () => new HostedMcpServerTool(null!, "https://localhost/"));
        Assert.Throws<ArgumentNullException>("serverName", () => new HostedMcpServerTool(null!, new Uri("https://localhost/")));

        Assert.Throws<ArgumentException>("serverAddress", () => new HostedMcpServerTool("name", string.Empty));
        Assert.Throws<ArgumentException>("serverUrl", () => new HostedMcpServerTool("name", new Uri("/api/mcp", UriKind.Relative)));
        Assert.Throws<ArgumentNullException>("serverAddress", () => new HostedMcpServerTool("name", (string)null!));
        Assert.Throws<ArgumentNullException>("serverUrl", () => new HostedMcpServerTool("name", (Uri)null!));
    }
}
