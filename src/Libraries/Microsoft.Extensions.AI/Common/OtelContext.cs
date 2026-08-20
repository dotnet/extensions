// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

// Shared source-generated JsonSerializerContext for the OpenTelemetry* clients.
// Registers the union of all OTel message-part types serialized by both OpenTelemetryChatClient
// and OpenTelemetryRealtimeClientSession.

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(IList<object>))]

// Shared types (Common/OtelMessageParts.cs)
[JsonSerializable(typeof(OtelGenericPart))]
[JsonSerializable(typeof(OtelBlobPart))]
[JsonSerializable(typeof(OtelUriPart))]
[JsonSerializable(typeof(OtelFilePart))]
[JsonSerializable(typeof(OtelToolCallResponsePart))]
[JsonSerializable(typeof(IEnumerable<OtelFunction>))]

// Latest experimental chat representation
[JsonSerializable(typeof(OtelLatest.Message))]
[JsonSerializable(typeof(OtelLatest.ToolCallRequestPart))]
[JsonSerializable(typeof(OtelServerToolCallPart<OtelLatest.CodeInterpreterToolCall>))]
[JsonSerializable(typeof(OtelServerToolCallResponsePart<OtelLatest.CodeInterpreterToolCallResponse>))]
[JsonSerializable(typeof(OtelServerToolCallPart<OtelLatest.ImageGenerationToolCall>))]
[JsonSerializable(typeof(OtelServerToolCallResponsePart<OtelLatest.ImageGenerationToolCallResponse>))]
[JsonSerializable(typeof(OtelServerToolCallPart<OtelMcpToolCall>))]
[JsonSerializable(typeof(OtelServerToolCallResponsePart<OtelMcpToolCallResponse>))]
[JsonSerializable(typeof(OtelServerToolCallPart<OtelLatest.McpApprovalRequest>))]
[JsonSerializable(typeof(OtelServerToolCallResponsePart<OtelLatest.McpApprovalResponse>))]

// v1.36 chat representation
[JsonSerializable(typeof(OtelV136.SystemOrUserEvent))]
[JsonSerializable(typeof(OtelV136.AssistantEvent))]
[JsonSerializable(typeof(OtelV136.ToolEvent))]
[JsonSerializable(typeof(OtelV136.ChoiceEvent))]

// Realtime-specific
[JsonSerializable(typeof(IEnumerable<RealtimeOtelMessage>))]
[JsonSerializable(typeof(RealtimeOtelMessage))]
[JsonSerializable(typeof(RealtimeOtelToolCallPart))]
internal sealed partial class OtelContext : JsonSerializerContext;
