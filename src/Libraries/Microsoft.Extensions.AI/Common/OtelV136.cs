// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Microsoft.Extensions.AI;

/// <summary>Serialization models for the GenAI semantic conventions v1.36 representation.</summary>
internal static class OtelV136
{
    internal sealed class SystemOrUserEvent
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    internal sealed class AssistantEvent
    {
        public string? Content { get; set; }
        public ToolCall[]? ToolCalls { get; set; }
    }

    internal sealed class ToolEvent
    {
        public string? Id { get; set; }
        public JsonNode? Content { get; set; }
    }

    internal sealed class ChoiceEvent
    {
        public string? FinishReason { get; set; }
        public int Index { get; set; }
        public AssistantEvent? Message { get; set; }
    }

    internal sealed class ToolCall
    {
        public string? Id { get; set; }
        public string Type { get; set; } = OpenTelemetryConsts.ToolTypeFunction;
        public ToolCallFunction? Function { get; set; }
    }

    internal sealed class ToolCallFunction
    {
        public string? Name { get; set; }
        public JsonNode? Arguments { get; set; }
    }
}
