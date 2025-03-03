// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats;
using static Microsoft.Extensions.AI.Evaluation.Reporting.Storage.DiskBasedResponseCache;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

[JsonSerializable(typeof(EvaluationResult))]
[JsonSerializable(typeof(Dataset))]
[JsonSerializable(typeof(CacheEntry))]
[JsonSerializable(typeof(CacheOptions))]
[JsonSourceGenerationOptions(
    Converters = [
        typeof(CamelCaseEnumConverter<EvaluationDiagnosticSeverity>),
        typeof(CamelCaseEnumConverter<EvaluationRating>),
        typeof(CamelCaseEnumConverter<CacheMode>),
        typeof(TimeSpanConverter)],
    WriteIndented = true,
    IgnoreReadOnlyProperties = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class SerializerContext : JsonSerializerContext
{
    private static SerializerContext? _compact;

    internal static SerializerContext Compact =>
        _compact ??=
            new(new JsonSerializerOptions(Default.Options)
            {
                WriteIndented = false,
            });
}
