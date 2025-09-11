// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;

public class KubernetesMetadata
{
    /// <summary>
    /// Gets or sets the resource memory limit the container is allowed to use.
    /// </summary>
    public ulong LimitsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU limit the container is allowed to use.
    /// </summary>
    public ulong LimitsCpu { get; set; }

    /// <summary>
    /// Gets or sets the resource memory request the container is allowed to use.
    /// </summary>
    public ulong RequestsMemory { get; set; }

    /// <summary>
    /// Gets or sets the resource CPU request the container is allowed to use.
    /// </summary>
    public ulong RequestsCpu { get; set; }

    private string _environmentVariablePrefix;

    public KubernetesMetadata(string environmentVariablePrefix)
    {
        _environmentVariablePrefix = environmentVariablePrefix;
    }

    public void Build()
    {
        LimitsMemory = Convert.ToUInt64(Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}LIMITS_MEMORY"), CultureInfo.InvariantCulture);
        LimitsCpu = Convert.ToUInt64(Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}LIMITS_CPU"), CultureInfo.InvariantCulture);
        RequestsMemory = Convert.ToUInt64(Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}REQUESTS_MEMORY"), CultureInfo.InvariantCulture);
        RequestsCpu = Convert.ToUInt64(Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}REQUESTS_CPU"), CultureInfo.InvariantCulture);
    }
}
