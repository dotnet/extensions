// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Source-generated JSON type information.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = true)]
[JsonSerializable(typeof(OpenAIRealtimeExtensions.ConversationFunctionToolParametersSchema))]
[JsonSerializable(typeof(OpenAIModelMappers.OpenAIChatToolJson))]
[JsonSerializable(typeof(IDictionary<string, object?>))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class OpenAIJsonContext : JsonSerializerContext;
