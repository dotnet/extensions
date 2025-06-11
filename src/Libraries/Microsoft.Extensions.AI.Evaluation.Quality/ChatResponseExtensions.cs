// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Quality;

internal static class ChatResponseExtensions
{
    internal static string RenderAsJson(this ChatResponse modelResponse, JsonSerializerOptions? options = null)
    {
        _ = Throw.IfNull(modelResponse);

        return modelResponse.Messages.RenderAsJson(options);
    }

    internal static string RenderToolCallsAndResultsAsJson(
        this ChatResponse modelResponse,
        JsonSerializerOptions? options = null)
    {
        _ = Throw.IfNull(modelResponse);

        var toolCallsAndResultsJsonArray = new JsonArray();

        foreach (AIContent content in modelResponse.Messages.SelectMany(m => m.Contents))
        {
            if (content is FunctionCallContent or FunctionResultContent)
            {
                Type contentType =
                    content is FunctionCallContent ? typeof(FunctionCallContent) : typeof(FunctionResultContent);

                JsonNode? toolCallOrResultJsonNode =
                    JsonSerializer.SerializeToNode(
                        content,
                        AIJsonUtilities.DefaultOptions.GetTypeInfo(contentType));

                if (toolCallOrResultJsonNode is not null)
                {
                    toolCallsAndResultsJsonArray.Add(toolCallOrResultJsonNode);
                }
            }
        }

        string renderedToolCallsAndResults = toolCallsAndResultsJsonArray.ToJsonString(options);
        return renderedToolCallsAndResults;
    }
}
