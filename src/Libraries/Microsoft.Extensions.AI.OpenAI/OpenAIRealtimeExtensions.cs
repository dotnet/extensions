// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Shared.Diagnostics;
using OpenAI.RealtimeConversation;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="RealtimeConversationSession"/> and related types.
/// </summary>
public static class OpenAIRealtimeExtensions
{
    /// <summary>
    /// Converts a <see cref="AIFunction"/> into a <see cref="ConversationFunctionTool"/> so that
    /// it can be used with <see cref="RealtimeConversationClient"/>.
    /// </summary>
    /// <returns>A <see cref="ConversationFunctionTool"/> that can be used with <see cref="RealtimeConversationClient"/>.</returns>
    public static ConversationFunctionTool ToConversationFunctionTool(this AIFunction aiFunction)
    {
        _ = Throw.IfNull(aiFunction);

        var parametersSchema = new ConversationFunctionToolParametersSchema
        {
            Type = "object",
            Properties = aiFunction.Metadata.Parameters
                .Where(p => p.Schema is JsonElement)
                .ToDictionary(p => p.Name, p => (JsonElement)p.Schema!),
            Required = aiFunction.Metadata.Parameters
                .Where(p => p.IsRequired)
                .Select(p => p.Name),
        };

        return new ConversationFunctionTool
        {
            Name = aiFunction.Metadata.Name,
            Description = aiFunction.Metadata.Description,
            Parameters = new BinaryData(JsonSerializer.SerializeToUtf8Bytes(
                parametersSchema, OpenAIJsonContext.Default.ConversationFunctionToolParametersSchema))
        };
    }

    internal sealed class ConversationFunctionToolParametersSchema
    {
        public string? Type { get; set; }
        public IDictionary<string, JsonElement>? Properties { get; set; }
        public IEnumerable<string>? Required { get; set; }
    }
}
