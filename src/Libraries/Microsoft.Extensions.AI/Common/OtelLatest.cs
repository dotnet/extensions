// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>Serialization models for the latest experimental GenAI semantic convention representation.</summary>
internal static class OtelLatest
{
    internal sealed class Message
    {
        public string? Role { get; set; }
        public string? Name { get; set; }
        public List<object> Parts { get; set; } = [];
        public string? FinishReason { get; set; }
    }

    internal sealed class ToolCallRequestPart
    {
        public string Type { get; set; } = "tool_call";
        public string? Id { get; set; }
        public string? Name { get; set; }
        public IDictionary<string, object?>? Arguments { get; set; }
    }

    internal sealed class CodeInterpreterToolCall
    {
        public string Type { get; set; } = "code_interpreter";
        public string? Code { get; set; }
    }

    internal sealed class CodeInterpreterToolCallResponse
    {
        public string Type { get; set; } = "code_interpreter";
        public object? Output { get; set; }
    }

    internal sealed class ImageGenerationToolCall
    {
        public string Type { get; set; } = "image_generation";
    }

    internal sealed class ImageGenerationToolCallResponse
    {
        public string Type { get; set; } = "image_generation";
        public object? Output { get; set; }
    }

    internal sealed class McpApprovalRequest
    {
        public string Type { get; set; } = "mcp_approval_request";
        public string? ServerName { get; set; }
        public IDictionary<string, object?>? Arguments { get; set; }
    }

    internal sealed class McpApprovalResponse
    {
        public string Type { get; set; } = "mcp_approval_response";
        public bool Approved { get; set; }
    }
}
