// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ClusterMetadata.Kubernetes;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// <see cref="ConfigurationBuilder"/> extensions for Kubernetes metadata.
/// </summary>
public static class KubernetesClusterMetadataConfigurationBuilderExtensions
{
    private const string DefaultSectionName = "clustermetadata:kubernetes";

    /// <summary>
    /// Registers configuration provider for Kubernetes metadata.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="sectionName">Section name to save configuration into. Default set to "clustermetadata:kubernetes".</param>
    /// <param name="environmentVariablePrefix">A prefix for environment variable names that have Kubernetes cluster information.</param>
    /// <returns>The input configuration builder for call chaining.</returns>
    public static IConfigurationBuilder AddKubernetesClusterMetadata(this IConfigurationBuilder builder, string sectionName = DefaultSectionName, string environmentVariablePrefix = "")
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNullOrWhitespace(sectionName);
        _ = Throw.IfNull(environmentVariablePrefix);

        return builder.Add(new KubernetesClusterMetadataSource(sectionName, environmentVariablePrefix));
    }
}
