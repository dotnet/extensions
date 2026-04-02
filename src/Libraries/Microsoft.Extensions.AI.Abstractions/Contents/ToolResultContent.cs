// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the result of a tool call.
/// </summary>
[JsonDerivedType(typeof(FunctionResultContent), "functionResult")]
[JsonDerivedType(typeof(McpServerToolResultContent), "mcpServerToolResult")]

// Same as in AIContent.
// These should be added in once they're no longer [Experimental]. If they're included while still
// experimental, any JsonSerializerContext that includes ToolResultContent will incur errors about using
// experimental types in its source generated files. When [Experimental] is removed from these types,
// these lines should be uncommented and the corresponding lines in AIJsonUtilities.CreateDefaultOptions
// as well as the [JsonSerializable] attributes for them on the JsonContext should be removed.
// [JsonDerivedType(typeof(CodeInterpreterToolResultContent), "codeInterpreterToolResult")]
// [JsonDerivedType(typeof(ImageGenerationToolResultContent), "imageGenerationToolResult")]
// [JsonDerivedType(typeof(WebSearchToolResultContent), "webSearchToolResult")]
public class ToolResultContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolResultContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID for which this is the result.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
    public ToolResultContent(string callId)
    {
        CallId = Throw.IfNull(callId);
    }

    /// <summary>
    /// Gets the ID of the tool call for which this is the result.
    /// </summary>
    /// <remarks>
    /// If this is the result for a <see cref="ToolCallContent"/>, this property should contain the same
    /// <see cref="ToolCallContent.CallId"/> value.
    /// </remarks>
    public string CallId { get; }

    /// <summary>
    /// Gets or sets the output contents of the tool call.
    /// </summary>
    /// <remarks>
    /// Outputs can include various types of content such as <see cref="TextContent"/> for text output,
    /// <see cref="DataContent"/> for binary data, <see cref="UriContent"/> for URL references,
    /// <see cref="HostedFileContent"/> for provider-hosted files, <see cref="ErrorContent"/> for errors,
    /// or other <see cref="AIContent"/> types as supported by the service.
    /// </remarks>
    public IList<AIContent>? Outputs { get; set; }
}
