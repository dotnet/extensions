// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using static Microsoft.Extensions.AI.Evaluation.Reporting.Storage.AzureStorageResponseCache;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

[JsonSerializable(typeof(ScenarioRunResult))]
[JsonSerializable(typeof(CacheEntry))]
[JsonSourceGenerationOptions(
    Converters = [
        typeof(AzureStorageCamelCaseEnumConverter<EvaluationDiagnosticSeverity>),
        typeof(AzureStorageCamelCaseEnumConverter<EvaluationRating>),
        typeof(AzureStorageTimeSpanConverter)],
    WriteIndented = true,
    IgnoreReadOnlyProperties = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class AzureStorageSerializerContext : JsonSerializerContext
{
    private static AzureStorageSerializerContext? _compact;

    internal static AzureStorageSerializerContext Compact =>
        _compact ??=
            new(new JsonSerializerOptions(Default.Options)
            {
                WriteIndented = false,
            });
}
