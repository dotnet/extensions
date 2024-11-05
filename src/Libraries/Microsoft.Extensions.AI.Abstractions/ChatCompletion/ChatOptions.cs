// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for a chat request.</summary>
public class ChatOptions
{
    /// <summary>Gets or sets the temperature for generating chat responses.</summary>
    public float? Temperature { get; set; }

    /// <summary>Gets or sets the maximum number of tokens in the generated chat response.</summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>Gets or sets the "nucleus sampling" factor (or "top p") for generating chat responses.</summary>
    public float? TopP { get; set; }

    /// <summary>Gets or sets a count indicating how many of the most probable tokens the model should consider when generating the next part of the text.</summary>
    public int? TopK { get; set; }

    /// <summary>Gets or sets the frequency penalty for generating chat responses.</summary>
    public float? FrequencyPenalty { get; set; }

    /// <summary>Gets or sets the presence penalty for generating chat responses.</summary>
    public float? PresencePenalty { get; set; }

    /// <summary>Gets or sets a seed value used by a service to control the reproducability of results.</summary>
    public long? Seed { get; set; }

    /// <summary>
    /// Gets or sets the response format for the chat request.
    /// </summary>
    /// <remarks>
    /// If null, no response format is specified and the client will use its default.
    /// This may be set to <see cref="ChatResponseFormat.Text"/> to specify that the response should be unstructured text,
    /// to <see cref="ChatResponseFormat.Json"/> to specify that the response should be structured JSON data, or
    /// an instance of <see cref="ChatResponseFormatJson"/> constructed with a specific JSON schema to request that the
    /// response be structured JSON data according to that schema. It is up to the client implementation if or how
    /// to honor the request. If the client implementation doesn't recognize the specific kind of <see cref="ChatResponseFormat"/>,
    /// it may be ignored.
    /// </remarks>
    public ChatResponseFormat? ResponseFormat { get; set; }

    /// <summary>Gets or sets the model ID for the chat request.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets the stop sequences for generating chat responses.</summary>
    public IList<string>? StopSequences { get; set; }

    /// <summary>Gets or sets the tool mode for the chat request.</summary>
    public ChatToolMode ToolMode { get; set; } = ChatToolMode.Auto;

    /// <summary>Gets or sets the list of tools to include with a chat request.</summary>
    [JsonIgnore]
    public IList<AITool>? Tools { get; set; }

    /// <summary>Gets or sets any additional properties associated with the options.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Produces a clone of the current <see cref="ChatOptions"/> instance.</summary>
    /// <returns>A clone of the current <see cref="ChatOptions"/> instance.</returns>
    /// <remarks>
    /// The clone will have the same values for all properties as the original instance. Any collections, like <see cref="Tools"/>,
    /// <see cref="StopSequences"/>, and <see cref="AdditionalProperties"/>, are shallow-cloned, meaning a new collection instance is created,
    /// but any references contained by the collections are shared with the original.
    /// </remarks>
    public virtual ChatOptions Clone()
    {
        ChatOptions options = new()
        {
            Temperature = Temperature,
            MaxOutputTokens = MaxOutputTokens,
            TopP = TopP,
            TopK = TopK,
            FrequencyPenalty = FrequencyPenalty,
            PresencePenalty = PresencePenalty,
            Seed = Seed,
            ResponseFormat = ResponseFormat,
            ModelId = ModelId,
            ToolMode = ToolMode,
            AdditionalProperties = AdditionalProperties?.Clone(),
        };

        if (StopSequences is not null)
        {
            options.StopSequences = new List<string>(StopSequences);
        }

        if (Tools is not null)
        {
            options.Tools = new List<AITool>(Tools);
        }

        return options;
    }
}
