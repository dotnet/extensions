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
