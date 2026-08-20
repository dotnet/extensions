// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;

namespace Microsoft.Extensions.AI;

/// <summary>Provides internal helpers for implementing telemetry.</summary>
internal static class TelemetryHelpers
{
    /// <summary>Gets a value indicating whether the OpenTelemetry clients should enable their EnableSensitiveData property's by default.</summary>
    /// <remarks>Defaults to false. May be overridden by setting the OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT environment variable to "true".</remarks>
    public static bool EnableSensitiveDataDefault { get; } =
        Environment.GetEnvironmentVariable(OpenTelemetryConsts.GenAICaptureMessageContentEnvVar) is string envVar &&
        string.Equals(envVar, "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Gets the default GenAI semantic convention representation for a new instrumentation instance.</summary>
    public static OpenTelemetryGenAISemanticConvention GetGenAISemanticConventionDefault() =>
        GetGenAISemanticConvention(Environment.GetEnvironmentVariable(OpenTelemetryConsts.SemanticConventionStabilityOptInEnvVar));

    /// <summary>Resolves the GenAI semantic convention representation from an OpenTelemetry stability opt-in list.</summary>
    internal static OpenTelemetryGenAISemanticConvention GetGenAISemanticConvention(string? stabilityOptIn)
    {
        if (stabilityOptIn is null)
        {
            return OpenTelemetryGenAISemanticConvention.LatestExperimental;
        }

        int startIndex = 0;
        while (startIndex <= stabilityOptIn.Length)
        {
            int separatorIndex = stabilityOptIn.IndexOf(',', startIndex);
            string value = separatorIndex >= 0 ?
                stabilityOptIn.Substring(startIndex, separatorIndex - startIndex) :
                stabilityOptIn.Substring(startIndex);

            if (string.Equals(value.Trim(), OpenTelemetryConsts.GenAILatestExperimentalOptIn, StringComparison.Ordinal))
            {
                return OpenTelemetryGenAISemanticConvention.LatestExperimental;
            }

            if (separatorIndex < 0)
            {
                break;
            }

            startIndex = separatorIndex + 1;
        }

        return OpenTelemetryGenAISemanticConvention.Version1_36;
    }

    /// <summary>Gets the provider attribute name for the selected GenAI semantic convention representation.</summary>
    public static string GetGenAIProviderAttributeName(OpenTelemetryGenAISemanticConvention semanticConvention) =>
        semanticConvention == OpenTelemetryGenAISemanticConvention.Version1_36 ?
            OpenTelemetryConsts.GenAI.SystemName :
            OpenTelemetryConsts.GenAI.Provider.Name;

    /// <summary>Serializes <paramref name="value"/> as JSON for logging purposes.</summary>
    public static string AsJson<T>(T value, JsonSerializerOptions? options)
    {
        if (options?.TryGetTypeInfo(typeof(T), out var typeInfo) is true ||
            AIJsonUtilities.DefaultOptions.TryGetTypeInfo(typeof(T), out typeInfo))
        {
            try
            {
                return JsonSerializer.Serialize(value, typeInfo);
            }
            catch
            {
                // If we fail to serialize, just fall through to returning "{}".
            }
        }

        // If we're unable to get a type info for the value, or if we fail to serialize,
        // return an empty JSON object. We do not want lack of type info to disrupt application behavior with exceptions.
        return "{}";
    }
}
