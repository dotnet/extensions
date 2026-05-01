// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.Extensions.AI;

/// <summary>OTel function-tool definition shared between the chat and realtime clients.</summary>
internal sealed class OtelFunction
{
    public string Type { get; set; } = "function";
    public string? Name { get; set; }
    public string? Description { get; set; }
    public JsonElement? Parameters { get; set; }

    /// <summary>Builds an <see cref="OtelFunction"/> from an <see cref="AITool"/>.</summary>
    /// <param name="tool">The tool to describe.</param>
    /// <param name="includeOptionalProperties">
    /// When <see langword="false"/>, the optional <see cref="Description"/> and <see cref="Parameters"/>
    /// properties will be set to <see langword="null"/>, as they may contain sensitive, user-authored
    /// values or large payloads.
    /// </param>
    public static OtelFunction Create(AITool tool, bool includeOptionalProperties)
    {
        if (tool.GetService<AIFunctionDeclaration>() is { } function)
        {
            return new()
            {
                Name = function.Name,
                Description = includeOptionalProperties ? function.Description : null,
                Parameters = includeOptionalProperties ? function.JsonSchema : null,
            };
        }

        return new()
        {
            Type = tool.Name,
            Name = tool.Name,
        };
    }
}
