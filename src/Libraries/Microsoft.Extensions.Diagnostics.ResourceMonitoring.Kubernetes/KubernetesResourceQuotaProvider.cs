// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;

internal class KubernetesResourceQuotaProvider : IResourceQuotaProvider
{
    private const double MillicoresPerCore = 1000.0;
    private KubernetesMetadata _cosmicMetadata;

    public KubernetesResourceQuotaProvider(KubernetesMetadata cosmicMetadata)
    {
        _cosmicMetadata = cosmicMetadata;
    }

    public ResourceQuota GetResourceQuota()
    {
        return new ResourceQuota
        {
            RequestsCpu = ConvertMillicoreToCpuUnit(_cosmicMetadata.RequestsCpu),
            LimitsCpu = ConvertMillicoreToCpuUnit(_cosmicMetadata.LimitsCpu),
            RequestsMemory = _cosmicMetadata.RequestsMemory,
            LimitsMemory = _cosmicMetadata.LimitsMemory,
        };
    }

    private static double ConvertMillicoreToCpuUnit(ulong millicores)
    {
        return millicores / MillicoresPerCore;
    }
}
