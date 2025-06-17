// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Represents the options for a chat request.</summary>
/// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#provide-options">Provide options.</related>
public class ChatOptions
{
    /// <summary>Gets or sets an optional identifier used to associate a request with an existing conversation.</summary>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#stateless-vs-stateful-clients">Stateless vs. stateful clients.</related>
    public string? ConversationId { get; set; }

    /// <summary>Gets or sets additional per-request instructions to be provided to the <see cref="IChatClient"/>.</summary>
    public string? Instructions { get; set; }

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
    /// If <see langword="null"/>, no response format is specified and the client will use its default.
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

    /// <summary>
    /// Gets or sets a flag to indicate whether a single response is allowed to include multiple tool calls.
    /// If <see langword="false"/>, the <see cref="IChatClient"/> is asked to return a maximum of one tool call per request.
    /// If <see langword="true"/>, there is no limit.
    /// If <see langword="null"/>, the provider may select its own default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When used with function calling middleware, this does not affect the ability to perform multiple function calls in sequence.
    /// It only affects the number of function calls within a single iteration of the function calling loop.
    /// </para>
    /// <para>
    /// The underlying provider is not guaranteed to support or honor this flag. For example it may choose to ignore it and return multiple tool calls regardless.
    /// </para>
    /// </remarks>
    public bool? AllowMultipleToolCalls { get; set; }

    /// <summary>Gets or sets the tool mode for the chat request.</summary>
    /// <remarks>The default value is <see langword="null"/>, which is treated the same as <see cref="ChatToolMode.Auto"/>.</remarks>
    public ChatToolMode? ToolMode { get; set; }

    /// <summary>Gets or sets the list of tools to include with a chat request.</summary>
    /// <related type="Article" href="https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai#tool-calling">Tool calling.</related>
    [JsonIgnore]
    public IList<AITool>? Tools { get; set; }

    /// <summary>
    /// Gets or sets a callback responsible for creating the raw representation of the chat options from an underlying implementation.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="IChatClient" /> implementation may have its own representation of options.
    /// When <see cref="IChatClient.GetResponseAsync" /> or <see cref="IChatClient.GetStreamingResponseAsync" />
    /// is invoked with a <see cref="ChatOptions" />, that implementation may convert the provided options into
    /// its own representation in order to use it while performing the operation. For situations where a consumer knows
    /// which concrete <see cref="IChatClient" /> is being used and how it represents options, a new instance of that
    /// implementation-specific options type may be returned by this callback, for the <see cref="IChatClient" />
    /// implementation to use instead of creating a new instance. Such implementations may mutate the supplied options
    /// instance further based on other settings supplied on this <see cref="ChatOptions" /> instance or from other inputs,
    /// like the enumerable of <see cref="ChatMessage"/>s, therefore, it is <b>strongly recommended</b> to not return shared instances
    /// and instead make the callback return a new instance on each call.
    /// This is typically used to set an implementation-specific setting that isn't otherwise exposed from the strongly-typed
    /// properties on <see cref="ChatOptions" />.
    /// </remarks>
    [JsonIgnore]
    public Func<IChatClient, object?>? RawRepresentationFactory { get; set; }

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
            AdditionalProperties = AdditionalProperties?.Clone(),
            AllowMultipleToolCalls = AllowMultipleToolCalls,
            ConversationId = ConversationId,
            FrequencyPenalty = FrequencyPenalty,
            Instructions = Instructions,
            MaxOutputTokens = MaxOutputTokens,
            ModelId = ModelId,
            PresencePenalty = PresencePenalty,
            RawRepresentationFactory = RawRepresentationFactory,
            ResponseFormat = ResponseFormat,
            Seed = Seed,
            Temperature = Temperature,
            ToolMode = ToolMode,
            TopK = TopK,
            TopP = TopP,
        };

        if (StopSequences is not null)
        {
            options.StopSequences = [.. StopSequences];
        }

        if (Tools is not null)
        {
            options.Tools = [.. Tools];
        }

        return options;
    }

    /// <summary>Merges the options specified by <paramref name="other"/> into this instance.</summary>
    /// <param name="other">The other options to be merged into this instance.</param>
    /// <param name="overwrite">
    /// If <see langword="true"/>, properties on this instance will be overwritten by the values from <paramref name="other"/>;
    /// if <see langword="false"/>, properties on this instance will only be updated if they are <see langword="null"/> on this instance.
    /// The default is <see langword="false"/>.
    /// </param>
    /// <remarks>
    /// Merging works by copying the values from <paramref name="other"/> into this instance.
    /// <para>
    /// When <paramref name="overwrite"/> is <see langword="false"/> (the default):
    /// <list type="bullet">
    ///   <item>
    ///     <description>For properties of primitive types (like <see cref="Temperature"/> or <see cref="MaxOutputTokens"/>), 
    ///     properties on this instance will only be updated if they are <see langword="null"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>For list types (like <see cref="StopSequences"/> or <see cref="Tools"/>), 
    ///     the elements of <paramref name="other"/>'s collection will be added into the corresponding collection 
    ///     on this instance (no deduplication is performed).</description>
    ///   </item>
    ///   <item>
    ///     <description>For dictionary types (like <see cref="AdditionalProperties"/>), a shallow copy is performed 
    ///     on the entries from <paramref name="other"/>, adding them into the corresponding dictionary on this instance, 
    ///     but only if the key does not already exist in this instance's dictionary.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// <para>
    /// When <paramref name="overwrite"/> is <see langword="true"/>, any non-<see langword="null"/> properties on <paramref name="other"/> will
    /// overwrite the corresponding properties on this instance.
    /// <list type="bullet">
    ///   <item>
    ///     <description>For properties of primitive types (like <see cref="Temperature"/> or <see cref="MaxOutputTokens"/>), 
    ///     properties on this instance will be updated with values from <paramref name="other"/> if they're non-<see langword="null"/>
    ///     on <paramref name="other"/>.</description>
    ///   </item>
    ///   <item>
    ///     <description>For list types (like <see cref="StopSequences"/> or <see cref="Tools"/>), 
    ///     values from <paramref name="other"/> will replace any existing collections on this instance if the collection
    ///     is non-<see langword="null"/> on <paramref name="other"/></description>
    ///   </item>
    ///   <item>
    ///     <description>For dictionary types (like <see cref="AdditionalProperties"/>), entries from <paramref name="other"/> 
    ///     will be stored into this instance's dictionary, overwriting any entries with the same key in this instance's dictionary.</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </remarks>
    public virtual void Merge(ChatOptions? other, bool overwrite = false)
    {
        if (other is not null)
        {
            if (overwrite)
            {
                MergeOverwrite(other);
            }
            else
            {
                MergeDefault(other);
            }
        }
    }

    private void MergeDefault(ChatOptions other)
    {
        if (other.AdditionalProperties is { Count: > 0 })
        {
            if (AdditionalProperties is null)
            {
                AdditionalProperties = other.AdditionalProperties.Clone();
            }
            else
            {
                foreach (var entry in other.AdditionalProperties)
                {
                    _ = AdditionalProperties.TryAdd(entry.Key, entry.Value);
                }
            }
        }

        AllowMultipleToolCalls ??= other.AllowMultipleToolCalls;

        ConversationId ??= other.ConversationId;

        FrequencyPenalty ??= other.FrequencyPenalty;

        Instructions ??= other.Instructions;

        MaxOutputTokens ??= other.MaxOutputTokens;

        ModelId ??= other.ModelId;

        PresencePenalty ??= other.PresencePenalty;

        if (other.RawRepresentationFactory is { } otherRrf)
        {
            RawRepresentationFactory = RawRepresentationFactory is { } originalRrf ?
                client => originalRrf(client) ?? otherRrf(client) :
                otherRrf;
        }

        ResponseFormat ??= other.ResponseFormat;

        Seed ??= other.Seed;

        if (other.StopSequences is not null)
        {
            if (StopSequences is null)
            {
                StopSequences = [.. other.StopSequences];
            }
            else if (StopSequences is List<string> stopSequences)
            {
                stopSequences.AddRange(other.StopSequences);
            }
            else
            {
                foreach (var sequence in other.StopSequences)
                {
                    StopSequences.Add(sequence);
                }
            }
        }

        Temperature ??= other.Temperature;

        ToolMode ??= other.ToolMode;

        if (other.Tools is not null)
        {
            if (Tools is null)
            {
                Tools = [.. other.Tools];
            }
            else if (Tools is List<AITool> tools)
            {
                tools.AddRange(other.Tools);
            }
            else
            {
                foreach (var tool in other.Tools)
                {
                    Tools.Add(tool);
                }
            }
        }

        TopK ??= other.TopK;

        TopP ??= other.TopP;
    }

    private void MergeOverwrite(ChatOptions other)
    {
        if (other.AdditionalProperties is { Count: > 0 })
        {
            if (AdditionalProperties is null)
            {
                AdditionalProperties = other.AdditionalProperties.Clone();
            }
            else
            {
                AdditionalProperties.SetAll(other.AdditionalProperties);
            }
        }

        if (other.AllowMultipleToolCalls is not null)
        {
            AllowMultipleToolCalls = other.AllowMultipleToolCalls;
        }

        if (other.ConversationId is not null)
        {
            ConversationId = other.ConversationId;
        }

        if (other.FrequencyPenalty is not null)
        {
            FrequencyPenalty = other.FrequencyPenalty;
        }

        if (other.Instructions is not null)
        {
            Instructions = other.Instructions;
        }

        if (other.MaxOutputTokens is not null)
        {
            MaxOutputTokens = other.MaxOutputTokens;
        }

        if (other.ModelId is not null)
        {
            ModelId = other.ModelId;
        }

        if (other.PresencePenalty is not null)
        {
            PresencePenalty = other.PresencePenalty;
        }

        if (other.RawRepresentationFactory is not null)
        {
            RawRepresentationFactory = other.RawRepresentationFactory;
        }

        if (other.ResponseFormat is not null)
        {
            ResponseFormat = other.ResponseFormat;
        }

        if (other.Seed is not null)
        {
            Seed = other.Seed;
        }

        if (other.StopSequences is not null)
        {
            if (StopSequences is null)
            {
                StopSequences = [.. other.StopSequences];
            }
            else
            {
                StopSequences.Clear();
                if (StopSequences is List<string> stopSequences)
                {
                    stopSequences.AddRange(other.StopSequences);
                }
                else
                {
                    foreach (var sequence in other.StopSequences)
                    {
                        StopSequences.Add(sequence);
                    }
                }
            }
        }

        if (other.Temperature is not null)
        {
            Temperature = other.Temperature;
        }

        if (other.ToolMode is not null)
        {
            ToolMode = other.ToolMode;
        }

        if (other.Tools is not null)
        {
            if (Tools is null)
            {
                Tools = [.. other.Tools];
            }
            else
            {
                Tools.Clear();
                if (Tools is List<AITool> tools)
                {
                    tools.AddRange(other.Tools);
                }
                else
                {
                    foreach (var tool in other.Tools)
                    {
                        Tools.Add(tool);
                    }
                }
            }
        }

        if (other.TopK is not null)
        {
            TopK = other.TopK;
        }

        if (other.TopP is not null)
        {
            TopP = other.TopP;
        }
    }
}
