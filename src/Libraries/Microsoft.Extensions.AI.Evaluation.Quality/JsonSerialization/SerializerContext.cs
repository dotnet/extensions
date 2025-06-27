// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI.Evaluation.Quality.JsonSerialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    AllowTrailingCommas = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(RelevanceTruthAndCompletenessRating))]
[JsonSerializable(typeof(IntentResolutionRating))]
internal sealed partial class SerializerContext : JsonSerializerContext;
