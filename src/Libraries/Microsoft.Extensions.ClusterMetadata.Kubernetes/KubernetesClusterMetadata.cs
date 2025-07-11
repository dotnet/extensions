// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.ClusterMetadata.Kubernetes;

/// <summary>
/// Maintains metadata about Kubernetes cluster.
/// </summary>
public class KubernetesClusterMetadata
{
    /// <summary>
    /// Gets or sets the name of the Kubernetes cluster.
    /// </summary>
    /// <value>
    /// Default value is an empty string.
    /// </value>
    [Required]
    public string ClusterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of a <c>CronJob</c> resource.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? CronJob { get; set; }

    /// <summary>
    /// Gets or sets the name of a <c>DaemonSet</c> resource.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? DaemonSet { get; set; }

    /// <summary>
    /// Gets or sets the name of a <c>Deployment</c> resource.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? Deployment { get; set; }

    /// <summary>
    /// Gets or sets the name of a <c>Job</c> resource.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? Job { get; set; }

    /// <summary>
    /// Gets or sets the name of a <c>namespace</c> where the service is deployed.
    /// </summary>
    /// <value>
    /// Default value is an empty string.
    /// </value>
    [Required]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of a <c>node</c> where the service pod is running.
    /// </summary>
    /// <value>
    /// Default value is an empty string.
    /// </value>
    [Required]
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of a <c>pod</c> which is running the code.
    /// </summary>
    /// <value>
    /// Default value is an empty string.
    /// </value>
    [Required]
    public string PodName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of a <c>ReplicaSet</c> resource.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? ReplicaSet { get; set; }

    /// <summary>
    /// Gets or sets the name of a <c>StatefulSet</c> resource.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? StatefulSet { get; set; }

    /// <summary>
    /// Gets or sets the name of an Azure cloud.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? AzureCloud { get; set; }

    /// <summary>
    /// Gets or sets the name of an Azure region.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? AzureRegion { get; set; }

    /// <summary>
    /// Gets or sets the name of an Azure geography.
    /// </summary>
    /// <value>
    /// Default value is <see langword="null" />.
    /// </value>
    public string? AzureGeography { get; set; }

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
}
