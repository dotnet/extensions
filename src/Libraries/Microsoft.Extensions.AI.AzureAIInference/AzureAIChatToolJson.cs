// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Used to create the JSON payload for an AzureAI chat tool description.</summary>
internal sealed class AzureAIChatToolJson
{
    /// <summary>Gets a singleton JSON data for empty parameters. Optimization for the reasonably common case of a parameterless function.</summary>
    public static BinaryData ZeroFunctionParametersSchema { get; } = new("""{"type":"object","required":[],"properties":{}}"""u8.ToArray());

    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = [];

    [JsonPropertyName("properties")]
    public Dictionary<string, JsonElement> Properties { get; set; } = [];
}
