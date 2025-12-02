// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Test;

internal class TestKubernetesEnvironmentSetup : IDisposable
{
    private const string ClusterLimitCpu = "TEST_SETUP_CLUSTER_";
    private readonly List<string> _environmentVariablesToCleanup = [];

    /// <summary>
    /// Sets up environment variables for Kubernetes resource monitoring tests.
    /// </summary>
    /// <param name="prefix">The prefix to use for environment variables. Defaults to "MYCLUSTER_".</param>
    /// <param name="limitsMemory">Memory limit in bytes. Defaults to 2GB.</param>
    /// <param name="limitsCpu">CPU limit in millicores. Defaults to 2000 (2 cores).</param>
    /// <param name="requestsMemory">Memory request in bytes. Defaults to 1GB.</param>
    /// <param name="requestsCpu">CPU request in millicores. Defaults to 1000 (1 core).</param>
    public void SetupKubernetesEnvironment(
        string? prefix = null,
        ulong limitsMemory = 2_147_483_648, // 2GB
        ulong limitsCpu = 2000, // 2 cores in millicores
        ulong requestsMemory = 1_073_741_824, // 1GB  
        ulong requestsCpu = 1000) // 1 core in millicores
    {
        prefix ??= ClusterLimitCpu;

        SetEnvironmentVariable($"{prefix}LIMITS_MEMORY", limitsMemory.ToString());
        SetEnvironmentVariable($"{prefix}LIMITS_CPU", limitsCpu.ToString());
        SetEnvironmentVariable($"{prefix}REQUESTS_MEMORY", requestsMemory.ToString());
        SetEnvironmentVariable($"{prefix}REQUESTS_CPU", requestsCpu.ToString());
    }

    /// <summary>
    /// Sets up minimal environment variables for Kubernetes resource monitoring tests without requests.
    /// </summary>
    /// <param name="prefix">The prefix to use for environment variables. Defaults to "MYCLUSTER_".</param>
    public void SetupMinimalKubernetesEnvironmentWithoutRequests(string? prefix = null)
    {
        prefix ??= ClusterLimitCpu;

        SetEnvironmentVariable($"{prefix}LIMITS_MEMORY", "1073741824"); // 1GB
        SetEnvironmentVariable($"{prefix}LIMITS_CPU", "1000"); // 1 core
    }

    /// <summary>
    /// Sets up environment variables for testing edge cases.
    /// </summary>
    /// <param name="prefix">The prefix to use for environment variables. Defaults to "MYCLUSTER_".</param>
    public void SetupEdgeCaseKubernetesEnvironment(string? prefix = null)
    {
        prefix ??= ClusterLimitCpu;

        // Set maximum values to test upper bounds
        SetEnvironmentVariable($"{prefix}LIMITS_MEMORY", ulong.MaxValue.ToString());
        SetEnvironmentVariable($"{prefix}LIMITS_CPU", ulong.MaxValue.ToString());
        SetEnvironmentVariable($"{prefix}REQUESTS_MEMORY", "0");
        SetEnvironmentVariable($"{prefix}REQUESTS_CPU", "0");
    }

    /// <summary>
    /// Sets an environment variable and tracks it for cleanup.
    /// </summary>
    /// <param name="name">The name of the environment variable.</param>
    /// <param name="value">The value to set.</param>
    public void SetEnvironmentVariable(string name, string value)
    {
        Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
        _environmentVariablesToCleanup.Add(name);
    }

    /// <summary>
    /// Clears all environment variables that were set during testing.
    /// </summary>
    public void ClearEnvironmentVariables()
    {
        foreach (string variableName in _environmentVariablesToCleanup)
        {
            Environment.SetEnvironmentVariable(variableName, null, EnvironmentVariableTarget.Process);
        }

        _environmentVariablesToCleanup.Clear();
    }

    public void Dispose()
    {
        ClearEnvironmentVariables();
    }
}

[CollectionDefinition("EnvironmentVariableTests", DisableParallelization = true)]
#pragma warning disable SA1402 // File may only contain a single type
public class KubernetesEnvironmentTestCollection
{
}
#pragma warning restore SA1402 // File may only contain a single type
