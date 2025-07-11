// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.ClusterMetadata.Kubernetes;

internal class KubernetesClusterMetadataSource : IConfigurationSource
{
    public string SectionName { get; }
    private KubernetesClusterMetadata KubernetesClusterMetadata { get; }
    private readonly string _environmentVariablePrefix;

    public KubernetesClusterMetadataSource(Func<KubernetesClusterMetadata> configure)
    {

    }

    public KubernetesClusterMetadataSource(string sectionName, string environmentVariablePrefix = "")
    {
        SectionName = Throw.IfNullOrWhitespace(sectionName);
        _environmentVariablePrefix = Throw.IfNull(environmentVariablePrefix);

        KubernetesClusterMetadata = InitializeKubernetesClusterMetadata();
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) => new MemoryConfigurationProvider(new MemoryConfigurationSource())
    {
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.ClusterName)}", KubernetesClusterMetadata.ClusterName },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.CronJob)}", KubernetesClusterMetadata.CronJob },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.DaemonSet)}", KubernetesClusterMetadata.DaemonSet },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.Deployment)}", KubernetesClusterMetadata.Deployment },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.Job)}", KubernetesClusterMetadata.Job },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.Namespace)}", KubernetesClusterMetadata.Namespace },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.NodeName)}", KubernetesClusterMetadata.NodeName },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.PodName)}", KubernetesClusterMetadata.PodName },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.ReplicaSet)}", KubernetesClusterMetadata.ReplicaSet },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.StatefulSet)}", KubernetesClusterMetadata.StatefulSet },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.AzureCloud)}", KubernetesClusterMetadata.AzureCloud },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.AzureRegion)}", KubernetesClusterMetadata.AzureRegion },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.AzureGeography)}", KubernetesClusterMetadata.AzureGeography },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.LimitsMemory)}", KubernetesClusterMetadata.LimitsMemory.ToString(CultureInfo.InvariantCulture) },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.LimitsCpu)}", KubernetesClusterMetadata.LimitsCpu.ToString(CultureInfo.InvariantCulture) },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.RequestsMemory)}", KubernetesClusterMetadata.RequestsMemory.ToString(CultureInfo.InvariantCulture) },
        { $"{SectionName}:{nameof(KubernetesClusterMetadata.RequestsCpu)}", KubernetesClusterMetadata.RequestsCpu.ToString(CultureInfo.InvariantCulture) },
    };

    private KubernetesClusterMetadata InitializeKubernetesClusterMetadata()
    {
        return new KubernetesClusterMetadata
        {
            ClusterName = GetEnvironmentVariableOrThrow("CLUSTER_NAME", "CLUSTERNAME"),
            CronJob = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}CRONJOB_NAME") ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}CRONJOBNAME"),
            DaemonSet = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}DAEMONSET_NAME")
                        ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}DAEMONSETNAME"),
            Deployment = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}DEPLOYMENT_NAME")
                         ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}DEPLOYMENTNAME"),
            Job = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}JOB_NAME") ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}JOBNAME"),
            Namespace = GetEnvironmentVariableOrThrow("NAMESPACE", environmentVariableAltName: null),
            NodeName = GetEnvironmentVariableOrThrow("NODE_NAME", "NODENAME"),
            PodName = GetEnvironmentVariableOrThrow("POD_NAME", "PODNAME"),
            ReplicaSet = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}REPLICASET_NAME")
                         ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}REPLICASETNAME"),
            StatefulSet = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}STATEFULSET_NAME")
                          ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}STATEFULSETNAME"),
            AzureCloud = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}AZURE_CLOUD")
                         ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}AZURECLOUD"),
            AzureRegion = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}AZURE_REGION")
                         ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}AZUREREGION"),
            AzureGeography = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}AZURE_GEOGRAPHY")
                         ?? Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}AZUREGEOGRAPHY"),

            LimitsMemory = Convert.ToUInt64(Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}LIMITS_MEMORY"), CultureInfo.InvariantCulture),
            LimitsCpu = Convert.ToUInt64(Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}LIMITS_CPU"), CultureInfo.InvariantCulture),
            RequestsMemory = Convert.ToUInt64(Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}REQUESTS_MEMORY"), CultureInfo.InvariantCulture),
            RequestsCpu = Convert.ToUInt64(Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}REQUESTS_CPU"), CultureInfo.InvariantCulture)
        };
    }

    private string GetEnvironmentVariableOrThrow(string environmentVariableName, string? environmentVariableAltName)
    {
        var result = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}{environmentVariableName}");

        if (result == null && environmentVariableAltName != null)
        {
            result = Environment.GetEnvironmentVariable($"{_environmentVariablePrefix}{environmentVariableAltName}");
        }

        return result ?? throw new InvalidOperationException($"Environment variable {_environmentVariablePrefix}{environmentVariableName} is not set.");
    }
}
