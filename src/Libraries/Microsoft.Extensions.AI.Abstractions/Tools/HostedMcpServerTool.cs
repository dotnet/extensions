// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a hosted MCP server tool that can be specified to an AI service.
/// </summary>
[Experimental("MEAI001")]
public class HostedMcpServerTool : AITool
{
    /// <summary>Any additional properties associated with the tool.</summary>
    private IReadOnlyDictionary<string, object?>? _additionalProperties;
    // <summary>Backing field for the <see cref="Headers"/> property.</summary>
    private IDictionary<string, string>? _headers;

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerTool"/> class.
    /// </summary>
    /// <param name="serverName">The name of the remote MCP server.</param>
    /// <param name="serverAddress">The address of the remote MCP server. This may be a URL, or in the case of a service providing built-in MCP servers with known names, it can be such a name.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serverName"/> or <paramref name="serverAddress"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="serverName"/> or <paramref name="serverAddress"/> is empty or composed entirely of whitespace.</exception>
    public HostedMcpServerTool(string serverName, string serverAddress)
    {
        ServerName = Throw.IfNullOrWhitespace(serverName);
        ServerAddress = Throw.IfNullOrWhitespace(serverAddress);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerTool"/> class.
    /// </summary>
    /// <param name="serverName">The name of the remote MCP server.</param>
    /// <param name="serverUrl">The URL of the remote MCP server.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serverName"/> or <paramref name="serverUrl"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="serverName"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="serverUrl"/> is not an absolute URL.</exception>
    public HostedMcpServerTool(string serverName, Uri serverUrl)
        : this(serverName, ValidateUrl(serverUrl))
    {
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerTool"/> class.
    /// </summary>
    /// <param name="serverName">The name of the remote MCP server.</param>
    /// <param name="serverAddress">The address of the remote MCP server. This may be a URL, or in the case of a service providing built-in MCP servers with known names, it can be such a name.</param>
    /// <param name="additionalProperties">Any additional properties associated with the tool.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serverName"/> or <paramref name="serverAddress"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="serverName"/> or <paramref name="serverAddress"/> is empty or composed entirely of whitespace.</exception>
    public HostedMcpServerTool(string serverName, string serverAddress, IReadOnlyDictionary<string, object?>? additionalProperties)
        : this(serverName, serverAddress)
    {
        _additionalProperties = additionalProperties;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedMcpServerTool"/> class.
    /// </summary>
    /// <param name="serverName">The name of the remote MCP server.</param>
    /// <param name="serverUrl">The URL of the remote MCP server.</param>
    /// <param name="additionalProperties">Any additional properties associated with the tool.</param>
    /// <exception cref="ArgumentNullException"><paramref name="serverName"/> or <paramref name="serverUrl"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="serverName"/> is empty or composed entirely of whitespace.</exception>
    /// <exception cref="ArgumentException"><paramref name="serverUrl"/> is not an absolute URL.</exception>
    public HostedMcpServerTool(string serverName, Uri serverUrl, IReadOnlyDictionary<string, object?>? additionalProperties)
        : this(serverName, ValidateUrl(serverUrl))
    {
        _additionalProperties = additionalProperties;
    }

    private static string ValidateUrl(Uri serverUrl)
    {
        _ = Throw.IfNull(serverUrl);

        if (!serverUrl.IsAbsoluteUri)
        {
            Throw.ArgumentException(nameof(serverUrl), "The provided URL is not absolute.");
        }

        return serverUrl.AbsoluteUri;
    }

    /// <inheritdoc />
    public override string Name => "mcp";

    /// <inheritdoc />
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => _additionalProperties ?? base.AdditionalProperties;

    /// <summary>
    /// Gets the name of the remote MCP server that is used to identify it.
    /// </summary>
    public string ServerName { get; }

    /// <summary>
    /// Gets the address of the remote MCP server. This may be a URL, or in the case of a service providing built-in MCP servers with known names, it can be such a name.
    /// </summary>
    public string ServerAddress { get; }

    /// <summary>
    /// Gets or sets the OAuth authorization token that the AI service should use when calling the remote MCP server.
    /// </summary>
    /// <remarks>
    /// When set, this value is automatically added to the <see cref="Headers"/> dictionary with the key "Authorization" 
    /// and the value "Bearer {token}". Setting this property will overwrite any existing "Authorization" header in <see cref="Headers"/>.
    /// Setting this property to <see langword="null"/> will remove the "Authorization" header from <see cref="Headers"/>.
    /// </remarks>
    public string? AuthorizationToken
    {
        get
        {
            if (Headers.TryGetValue("Authorization", out string? value) && value.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                return value.Substring("Bearer ".Length);
            }

            return null;
        }
        set
        {
            if (value is not null)
            {
                Headers["Authorization"] = $"Bearer {value}";
            }
            else
            {
                _ = Headers.Remove("Authorization");
            }
        }
    }

    /// <summary>
    /// Gets or sets the description of the remote MCP server, used to provide more context to the AI service.
    /// </summary>
    public string? ServerDescription { get; set; }

    /// <summary>
    /// Gets or sets the list of tools allowed to be used by the AI service.
    /// </summary>
    /// <remarks>
    /// The default value is <see langword="null"/>, which allows any tool to be used.
    /// </remarks>
    public IList<string>? AllowedTools { get; set; }

    /// <summary>
    /// Gets or sets the approval mode that indicates when the AI service should require user approval for tool calls to the remote MCP server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You can set this property to <see cref="HostedMcpServerToolApprovalMode.AlwaysRequire"/> to require approval for all tool calls,
    /// or to <see cref="HostedMcpServerToolApprovalMode.NeverRequire"/> to never require approval.
    /// </para>
    /// <para>
    /// The default value is <see langword="null"/>, which some providers might treat the same as <see cref="HostedMcpServerToolApprovalMode.AlwaysRequire"/>.
    /// </para>
    /// <para>
    /// The underlying provider is not guaranteed to support or honor the approval mode.
    /// </para>
    /// </remarks>
    public HostedMcpServerToolApprovalMode? ApprovalMode { get; set; }

    /// <summary>
    /// Gets a mutable dictionary of HTTP headers to include when calling the remote MCP server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The underlying provider is not guaranteed to support or honor the headers.
    /// </para>
    /// </remarks>
    public IDictionary<string, string> Headers
    {
        get => _headers ??= new Dictionary<string, string>();
        set => _headers = value;
    }
}
