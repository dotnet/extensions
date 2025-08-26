// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ClusterMetadata.Kubernetes;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal sealed class KubernetesResourceQuotasProvider
{
    private readonly KubernetesClusterMetadata _kubernetesMetadata;

    public KubernetesResourceQuotasProvider(IOptions<KubernetesClusterMetadata> kubernetesMetadata)
    {
        _kubernetesMetadata = kubernetesMetadata.Value;
    }

    public KubernetesResourceQuotas GetResourceLimits()
    {
        double cpuRequest = ConvertMillicoreToUnit(_kubernetesMetadata.RequestsCpu);
        double cpuLimit = ConvertMillicoreToUnit(_kubernetesMetadata.LimitsCpu);
        ulong memoryRequest = _kubernetesMetadata.RequestsMemory;
        ulong memoryLimit = _kubernetesMetadata.LimitsMemory;

        if (cpuRequest <= 0)
        {
            Throw.InvalidOperationException($"REQUESTS_CPU detected value is {cpuRequest}, " +
                $"environment variables may be misconfigured");
        }

        if (cpuLimit <= 0)
        {
            Throw.InvalidOperationException($"LIMITS_CPU detected value is {cpuLimit}, " +
                $"environment variables may be misconfigured");
        }

        if (memoryRequest <= 0)
        {
            Throw.InvalidOperationException($"REQUESTS_MEMORY detected value is {memoryRequest}, " +
                $"environment variables may be misconfigured");
        }

        if (memoryLimit <= 0)
        {
            Throw.InvalidOperationException($"LIMITS_MEMORY detected value is {memoryLimit}, " +
                $"environment variables may be misconfigured");
        }

        return new KubernetesResourceQuotas(
            cpuLimit,
            cpuRequest,
            memoryLimit,
            memoryRequest);
    }

    private static double ConvertMillicoreToUnit(ulong millicores)
    {
        return millicores / 1000.0;
    }
}
