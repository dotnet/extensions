// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;

internal sealed class KubernetesMetadata
{
    /// <summary>
    /// Gets or sets the resource memory limit the container is allowed to use in bytes.
    /// </summary>
    public ulong LimitsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU limit the container is allowed to use in millicores.
    /// </summary>
    public ulong LimitsCpu { get; set; }

    /// <summary>
    /// Gets or sets the resource memory request the container is allowed to use in bytes.
    /// </summary>
    public ulong RequestsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU request the container is allowed to use in millicores.
    /// </summary>
    public ulong RequestsCpu { get; set; }

    public static KubernetesMetadata FromEnvironmentVariables(string environmentVariablePrefix)
    {
        var limitsMemory = GetEnvironmentVariableAsUInt64($"{environmentVariablePrefix}LIMITS_MEMORY");
        var limitsCpu = GetEnvironmentVariableAsUInt64($"{environmentVariablePrefix}LIMITS_CPU");
        var requestsMemory = GetEnvironmentVariableAsUInt64($"{environmentVariablePrefix}REQUESTS_MEMORY");
        var requestsCpu = GetEnvironmentVariableAsUInt64($"{environmentVariablePrefix}REQUESTS_CPU");

        if (limitsMemory == 0)
        {
            throw new InvalidOperationException($"Environment variable '{environmentVariablePrefix}LIMITS_MEMORY' is required and cannot be zero or missing.");
        }

        if (limitsCpu == 0)
        {
            throw new InvalidOperationException($"Environment variable '{environmentVariablePrefix}LIMITS_CPU' is required and cannot be zero or missing.");
        }

        return new KubernetesMetadata
        {
            LimitsMemory = limitsMemory,
            LimitsCpu = limitsCpu,
            RequestsMemory = requestsMemory,
            RequestsCpu = requestsCpu,
        };
    }

    private static ulong GetEnvironmentVariableAsUInt64(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ulong result))
        {
            throw new InvalidOperationException($"Environment variable '{variableName}' contains invalid value '{value}'. Expected a non-negative integer.");
        }

        return result;
    }
}
