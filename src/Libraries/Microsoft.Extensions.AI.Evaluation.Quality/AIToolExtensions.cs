// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

internal static class AIToolExtensions
{
    internal static string RenderAsJson(
        this IEnumerable<AITool> toolDefinitions,
        JsonSerializerOptions? options = null)
    {
        _ = Throw.IfNull(toolDefinitions);

        var toolDefinitionsJsonArray = new JsonArray();

        foreach (AIFunction function in toolDefinitions.OfType<AIFunction>())
        {
            JsonNode functionJsonNode =
                new JsonObject
                {
                    ["name"] = function.Name,
                    ["description"] = function.Description,
                    ["functionSchema"] = JsonNode.Parse(function.JsonSchema.GetRawText()),
                };

            if (function.ReturnJsonSchema is not null)
            {
                functionJsonNode["functionReturnValueSchema"] =
                    JsonNode.Parse(function.ReturnJsonSchema.Value.GetRawText());
            }

            toolDefinitionsJsonArray.Add(functionJsonNode);
        }

        string renderedToolDefinitions = toolDefinitionsJsonArray.ToJsonString(options);
        return renderedToolDefinitions;
    }
}
