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
        Assert.NotNull(tool.Headers);
        Assert.Empty(tool.Headers);
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

        Assert.NotNull(tool.Headers);
        Assert.Single(tool.Headers);
        tool.Headers["X-Custom-Header"] = "value1";
        Assert.True(tool.Headers.Count == 2);
        Assert.Equal("value1", tool.Headers["X-Custom-Header"]);
    }

    [Fact]
    public void Constructor_WithHeaders_Uri_Roundtrips()
    {
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token456",
            ["X-Custom"] = "value2"
        };
        HostedMcpServerTool tool = new("serverName", new Uri("https://localhost/"));
        foreach (KeyValuePair<string, string> keyValuePair in headers)
        {
            tool.Headers[keyValuePair.Key] = keyValuePair.Value;
        }

        Assert.Equal("serverName", tool.ServerName);
        Assert.Equal("https://localhost/", tool.ServerAddress);
        Assert.Equal(2, tool.Headers.Count);
        Assert.Equal("Bearer token456", tool.Headers["Authorization"]);
        Assert.Equal("token456", tool.AuthorizationToken);
        Assert.Equal("value2", tool.Headers["X-Custom"]);
    }

    [Fact]
    public void Constructor_WithNullHeaders_CreatesEmptyDictionary()
    {
        HostedMcpServerTool tool1 = new("serverName", "connector_id");
        Assert.NotNull(tool1.Headers);
        Assert.Empty(tool1.Headers);

        HostedMcpServerTool tool2 = new("serverName", new Uri("https://localhost/"));
        Assert.NotNull(tool2.Headers);
        Assert.Empty(tool2.Headers);
    }

    [Fact]
    public void AuthorizationToken_And_Headers_NoOrderingIssues()
    {
        // Verify that setting AuthorizationToken followed by adding to Headers works
        var tool1 = new HostedMcpServerTool("server", "https://localhost/")
        {
            AuthorizationToken = "token123"
        };
        tool1.Headers["X-Custom"] = "value1";

        Assert.Equal(2, tool1.Headers.Count);
        Assert.Equal("Bearer token123", tool1.Headers["Authorization"]);
        Assert.Equal("token123", tool1.AuthorizationToken);
        Assert.Equal("value1", tool1.Headers["X-Custom"]);

        // Verify that adding to Headers followed by setting AuthorizationToken works the same
        var tool2 = new HostedMcpServerTool("server", "https://localhost/");
        tool2.Headers["X-Custom"] = "value1";
        tool2.AuthorizationToken = "token123";

        Assert.Equal(2, tool2.Headers.Count);
        Assert.Equal("Bearer token123", tool2.Headers["Authorization"]);
        Assert.Equal("token123", tool2.AuthorizationToken);
        Assert.Equal("value1", tool2.Headers["X-Custom"]);

        // Verify setting AuthorizationToken to null removes only Authorization header
        tool2.AuthorizationToken = null;
        Assert.Single(tool2.Headers);
        Assert.False(tool2.Headers.ContainsKey("Authorization"));
        Assert.Null(tool2.AuthorizationToken);
        Assert.Equal("value1", tool2.Headers["X-Custom"]);
    }

    [Fact]
    public void Headers_WithNullAuthorization()
    {
        var tool = new HostedMcpServerTool("server", "https://localhost/");
        tool.Headers["Authorization"] = null!;
        tool.Headers["X-Custom"] = "value1";
        Assert.Equal(2, tool.Headers.Count);
        Assert.Null(tool.Headers["Authorization"]);
        Assert.Null(tool.AuthorizationToken);
        Assert.Equal("value1", tool.Headers["X-Custom"]);
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

