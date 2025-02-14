// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for a chat request.</summary>
public class ChatOptions
{
    /// <summary>Gets or sets an optional identifier used to associate a request with an existing chat thread.</summary>
    public string? ChatThreadId { get; set; }

    /// <summary>Gets or sets the temperature for generating chat responses.</summary>
    /// <remarks>
    /// This value controls the randomness of predictions made by the model. Use a lower value to decrease randomness in the response.
    /// </remarks>
    public float? Temperature { get; set; }

    /// <summary>Gets or sets the maximum number of tokens in the generated chat response.</summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>Gets or sets the "nucleus sampling" factor (or "top p") for generating chat responses.</summary>
    /// <remarks>
    /// Nucleus sampling is an alternative to sampling with temperature where the model
    /// considers the results of the tokens with <see cref="TopP"/> probability mass.
    /// For example, 0.1 means only the tokens comprising the top 10% probability mass are considered.
    /// </remarks>
    public float? TopP { get; set; }

    /// <summary>
    /// Gets or sets the number of most probable tokens that the model considers when generating the next part of the text.
    /// </summary>
    /// <remarks>
    /// This property reduces the probability of generating nonsense. A higher value gives more diverse answers, while a lower value is more conservative.
    /// </remarks>
    public int? TopK { get; set; }

    /// <summary>
    /// Gets or sets the penalty for repeated tokens in chat responses proportional to how many times they've appeared.
    /// </summary>
    /// <remarks>
    /// You can modify this value to reduce the repetitiveness of generated tokens. The higher the value, the stronger a penalty
    /// is applied to previously present tokens, proportional to how many times they've already appeared in the prompt or prior generation.
    /// </remarks>
    public float? FrequencyPenalty { get; set; }

    /// <summary>
    /// Gets or sets a value that influences the probability of generated tokens appearing based on their existing presence in generated text.
    /// </summary>
    /// <remarks>
    /// You can modify this value to reduce repetitiveness of generated tokens. Similar to <see cref="FrequencyPenalty"/>,
    /// except that this penalty is applied equally to all tokens that have already appeared, regardless of their exact frequencies.
    /// </remarks>
    public float? PresencePenalty { get; set; }

    /// <summary>Gets or sets a seed value used by a service to control the reproducibility of results.</summary>
    public long? Seed { get; set; }

    /// <summary>
    /// Gets or sets the response format for the chat request.
    /// </summary>
    /// <remarks>
    /// If null, no response format is specified and the client will use its default.
    /// This property can be set to <see cref="ChatResponseFormat.Text"/> to specify that the response should be unstructured text,
    /// to <see cref="ChatResponseFormat.Json"/> to specify that the response should be structured JSON data, or
    /// an instance of <see cref="ChatResponseFormatJson"/> constructed with a specific JSON schema to request that the
    /// response be structured JSON data according to that schema. It is up to the client implementation if or how
    /// to honor the request. If the client implementation doesn't recognize the specific kind of <see cref="ChatResponseFormat"/>,
    /// it can be ignored.
    /// </remarks>
    public ChatResponseFormat? ResponseFormat { get; set; }

    /// <summary>Gets or sets the model ID for the chat request.</summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets the list of stop sequences.
    /// </summary>
    /// <remarks>
    /// After a stop sequence is detected, the model stops generating further tokens for chat responses.
    /// </remarks>
    public IList<string>? StopSequences { get; set; }

    /// <summary>Gets or sets the tool mode for the chat request.</summary>
    /// <remarks>The default value is <see langword="null"/>, which is treated the same as <see cref="ChatToolMode.Auto"/>.</remarks>
    public ChatToolMode? ToolMode { get; set; }

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
            ChatThreadId = ChatThreadId,
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
