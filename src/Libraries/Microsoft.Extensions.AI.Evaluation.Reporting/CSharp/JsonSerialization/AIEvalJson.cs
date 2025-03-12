// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats;
using static Microsoft.Extensions.AI.Evaluation.Reporting.Storage.DiskBasedResponseCache;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

internal static partial class AIEvalJson
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Default matches the generated source naming convention.")]
    internal static class Default
    {
        private static JsonSerializerOptions? _options;
        internal static JsonSerializerOptions Options => _options ??= CreateJsonSerializerOptions(writeIndented: false);
        internal static JsonTypeInfo<Dataset> Dataset => Options.GetTypeInfo<Dataset>();
        internal static JsonTypeInfo<CacheEntry> CacheEntry => Options.GetTypeInfo<CacheEntry>();
        internal static JsonTypeInfo<CacheOptions> CacheOptions => Options.GetTypeInfo<CacheOptions>();
        internal static JsonTypeInfo<ScenarioRunResult> ScenarioRunResult => Options.GetTypeInfo<ScenarioRunResult>();
    }

    internal static class Compact
    {
        private static JsonSerializerOptions? _options;
        internal static JsonSerializerOptions Options => _options ??= CreateJsonSerializerOptions(writeIndented: true);
        internal static JsonTypeInfo<Dataset> Dataset => Options.GetTypeInfo<Dataset>();
        internal static JsonTypeInfo<CacheEntry> CacheEntry => Options.GetTypeInfo<CacheEntry>();
        internal static JsonTypeInfo<CacheOptions> CacheOptions => Options.GetTypeInfo<CacheOptions>();
        internal static JsonTypeInfo<ScenarioRunResult> ScenarioRunResult => Options.GetTypeInfo<ScenarioRunResult>();
    }

    private static JsonTypeInfo<T> GetTypeInfo<T>(this JsonSerializerOptions options) => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));

    private static JsonSerializerOptions CreateJsonSerializerOptions(bool writeIndented)
    {
        var options = new JsonSerializerOptions(JsonContext.Default.Options)
        {
            WriteIndented = writeIndented,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        options.TypeInfoResolverChain.Add(AIJsonUtilities.DefaultOptions.TypeInfoResolver!);
        options.MakeReadOnly();
        return options;
    }

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
    private sealed partial class JsonContext : JsonSerializerContext;

}
