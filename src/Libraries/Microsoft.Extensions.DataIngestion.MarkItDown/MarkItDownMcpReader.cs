// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Reads documents by converting them to Markdown using the <see href="https://github.com/microsoft/markitdown">MarkItDown</see> MCP server.
/// </summary>
public class MarkItDownMcpReader : IngestionDocumentReader
{
    private readonly Uri _mcpServerUri;
    private readonly McpClientOptions? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkItDownMcpReader"/> class.
    /// </summary>
    /// <param name="mcpServerUri">The URI of the MarkItDown MCP server (e.g., http://localhost:3001/mcp).</param>
    /// <param name="options">Optional MCP client options for configuring the connection.</param>
    public MarkItDownMcpReader(Uri mcpServerUri, McpClientOptions? options = null)
    {
        _mcpServerUri = Throw.IfNull(mcpServerUri);
        _options = options;
    }

    /// <inheritdoc/>
    public override async Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(source);
        _ = Throw.IfNullOrEmpty(identifier);

        if (!source.Exists)
        {
            throw new FileNotFoundException("The specified file does not exist.", source.FullName);
        }

        // Read file content as base64 data URI
#if NET
        byte[] fileBytes = await File.ReadAllBytesAsync(source.FullName, cancellationToken).ConfigureAwait(false);
#else
        byte[] fileBytes;
        using (FileStream fs = new(source.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, 1, FileOptions.Asynchronous))
        {
            using MemoryStream ms = new();
            await fs.CopyToAsync(ms).ConfigureAwait(false);
            fileBytes = ms.ToArray();
        }
#endif
        string dataUri = CreateDataUri(fileBytes, mediaType);

        string markdown = await ConvertToMarkdownAsync(dataUri, cancellationToken).ConfigureAwait(false);

        return MarkdownParser.Parse(markdown, identifier);
    }

    /// <inheritdoc/>
    public override async Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(source);
        _ = Throw.IfNullOrEmpty(identifier);

        // Read stream content as base64 data URI
        using MemoryStream ms = new();
#if NET
        await source.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
#else
        await source.CopyToAsync(ms).ConfigureAwait(false);
#endif
        byte[] fileBytes = ms.ToArray();
        string dataUri = CreateDataUri(fileBytes, mediaType);

        string markdown = await ConvertToMarkdownAsync(dataUri, cancellationToken).ConfigureAwait(false);

        return MarkdownParser.Parse(markdown, identifier);
    }

    private static string CreateDataUri(byte[] fileBytes, string? mediaType)
    {
        string base64Content = Convert.ToBase64String(fileBytes);
        string mimeType = string.IsNullOrEmpty(mediaType) ? "application/octet-stream" : mediaType!;
        return $"data:{mimeType};base64,{base64Content}";
    }

    private async Task<string> ConvertToMarkdownAsync(string dataUri, CancellationToken cancellationToken)
    {
        // Create HTTP client transport for MCP
        await using var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = _mcpServerUri
        });

        // Create MCP client
        await using var client = await McpClient.CreateAsync(transport, _options, loggerFactory: null, cancellationToken).ConfigureAwait(false);

        // Build parameters for convert_to_markdown tool
        var parameters = new Dictionary<string, object?>
        {
            ["uri"] = dataUri
        };

        // Call the convert_to_markdown tool
        var result = await client.CallToolAsync("convert_to_markdown", parameters, cancellationToken: cancellationToken).ConfigureAwait(false);

        // Extract markdown content from result
        // The result is expected to be in the format: { "content": [{ "type": "text", "text": "markdown content" }] }
        if (result.Content != null && result.Content.Count > 0)
        {
            foreach (var content in result.Content)
            {
                if (content.Type == "text" && content is TextContentBlock textBlock)
                {
                    return textBlock.Text;
                }
            }
        }

        throw new InvalidOperationException("Failed to convert document to markdown: unexpected response format from MCP server.");
    }
}
