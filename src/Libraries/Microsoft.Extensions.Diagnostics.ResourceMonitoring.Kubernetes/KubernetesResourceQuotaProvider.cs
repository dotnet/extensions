// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;

internal class KubernetesResourceQuotaProvider : ResourceQuotaProvider
{
    private const double MillicoresPerCore = 1000.0;
    private KubernetesMetadata _kubernetesMetadata;

    public KubernetesResourceQuotaProvider(KubernetesMetadata kubernetesMetadata)
    {
        _kubernetesMetadata = kubernetesMetadata;
    }

    public override ResourceQuota GetResourceQuota()
    {
        return new ResourceQuota
        {
            GuaranteedCpuInCores = ConvertMillicoreToCpuUnit(_kubernetesMetadata.RequestsCpu),
            MaxCpuInCores = ConvertMillicoreToCpuUnit(_kubernetesMetadata.LimitsCpu),
            GuaranteedMemoryInBytes = _kubernetesMetadata.RequestsMemory,
            MaxMemoryInBytes = _kubernetesMetadata.LimitsMemory,
        };
    }

    private static double ConvertMillicoreToCpuUnit(ulong millicores)
    {
        return millicores / MillicoresPerCore;
    }
}
