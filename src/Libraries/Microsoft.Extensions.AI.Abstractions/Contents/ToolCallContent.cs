// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a tool call request.
/// </summary>
[JsonDerivedType(typeof(FunctionCallContent), "functionCall")]
[JsonDerivedType(typeof(McpServerToolCallContent), "mcpServerToolCall")]

// Same as in AIContent.
// These should be added in once they're no longer [Experimental]. If they're included while still
// experimental, any JsonSerializerContext that includes ToolCallContent will incur errors about using
// experimental types in its source generated files. When [Experimental] is removed from these types,
// these lines should be uncommented and the corresponding lines in AIJsonUtilities.CreateDefaultOptions
// as well as the [JsonSerializable] attributes for them on the JsonContext should be removed.
// [JsonDerivedType(typeof(CodeInterpreterToolCallContent), "codeInterpreterToolCall")]
// [JsonDerivedType(typeof(ImageGenerationToolCallContent), "imageGenerationToolCall")]
public class ToolCallContent : AIContent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallContent"/> class.
    /// </summary>
    /// <param name="callId">The tool call ID.</param>
    /// <exception cref="ArgumentNullException"><paramref name="callId"/> is <see langword="null"/>.</exception>
    protected ToolCallContent(string callId)
    {
        CallId = Throw.IfNull(callId);
    }

    /// <summary>
    /// Gets the tool call ID.
    /// </summary>
    public string CallId { get; }
}
