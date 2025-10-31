// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;

internal class KubernetesMetadata
{
    /// <summary>
    /// Gets or sets the resource memory limit the container is allowed to use in bytes.
    /// </summary>
    public ulong LimitsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU limit the container is allowed to use in milicores.
    /// </summary>
    public ulong LimitsCpu { get; set; }

    /// <summary>
    /// Gets or sets the resource memory request the container is allowed to use in bytes.
    /// </summary>
    public ulong RequestsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU request the container is allowed to use in milicores.
    /// </summary>
    public ulong RequestsCpu { get; set; }

    private string _environmentVariablePrefix;

    public KubernetesMetadata(string environmentVariablePrefix)
    {
        _environmentVariablePrefix = environmentVariablePrefix;
    }

    /// <summary>
    /// Fills the object with data loaded from environment variables.
    /// </summary>
    public void Build()
    {
        LimitsMemory = GetEnvironmentVariableAsUInt64($"{_environmentVariablePrefix}LIMITS_MEMORY");
        LimitsCpu = GetEnvironmentVariableAsUInt64($"{_environmentVariablePrefix}LIMITS_CPU");
        RequestsMemory = GetEnvironmentVariableAsUInt64($"{_environmentVariablePrefix}REQUESTS_MEMORY");
        RequestsCpu = GetEnvironmentVariableAsUInt64($"{_environmentVariablePrefix}REQUESTS_CPU");
    }

    private static ulong GetEnvironmentVariableAsUInt64(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Environment variable '{variableName}' is not set or is empty.");
        }

        if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out ulong result))
        {
            throw new InvalidOperationException($"Environment variable '{variableName}' contains invalid value '{value}'. Expected a non-negative integer.");
        }

        return result;
    }
}
