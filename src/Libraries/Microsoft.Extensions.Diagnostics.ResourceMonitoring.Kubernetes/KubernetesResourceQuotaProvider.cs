// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;

internal class KubernetesResourceQuotaProvider : ResourceQuotaProvider
{
    private const double MillicoresPerCore = 1000.0;
    private KubernetesMetadata _kubernetesMetadata;

    public KubernetesResourceQuotaProvider(KubernetesMetadata kubernetesMetadata)
    {
        _ = Throw.IfNull(kubernetesMetadata);
        _kubernetesMetadata = kubernetesMetadata;
    }

    public override ResourceQuota GetResourceQuota()
    {
        ResourceQuota quota = new()
        {
            BaselineCpuInCores = ConvertMillicoreToCpuUnit(_kubernetesMetadata.RequestsCpu),
            MaxCpuInCores = ConvertMillicoreToCpuUnit(_kubernetesMetadata.LimitsCpu),
            BaselineMemoryInBytes = _kubernetesMetadata.RequestsMemory,
            MaxMemoryInBytes = _kubernetesMetadata.LimitsMemory,
        };

        if (quota.BaselineCpuInCores <= 0.0)
        {
            quota.BaselineCpuInCores = quota.MaxCpuInCores;
        }

        if (quota.BaselineMemoryInBytes == 0)
        {
            quota.BaselineMemoryInBytes = quota.MaxMemoryInBytes;
        }

        return quota;
    }

    private static double ConvertMillicoreToCpuUnit(ulong millicores)
    {
        return millicores / MillicoresPerCore;
    }
}
