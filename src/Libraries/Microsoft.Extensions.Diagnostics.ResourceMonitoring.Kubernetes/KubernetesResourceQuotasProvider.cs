// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Kubernetes;

internal class KubernetesResourceQuotasProvider : IResourceQuotasProvider
{
    private KubernetesMetadata _cosmicMetadata;

    public KubernetesResourceQuotasProvider(KubernetesMetadata cosmicMetadata) // we could inject ClusterMetadataHere in the future instead of that thingy
    {
        _cosmicMetadata = cosmicMetadata;
    }

    public ResourceQuotas GetResourceQuotas()
    {
        // extract relevant cluster metadata
        return new ResourceQuotas
        {
            RequestsCpu = ConvertMillicoreToUnit(_cosmicMetadata.RequestsCpu),
            LimitsCpu = ConvertMillicoreToUnit(_cosmicMetadata.LimitsCpu),
            RequestsMemory = _cosmicMetadata.RequestsMemory,
            LimitsMemory = _cosmicMetadata.LimitsMemory,
        };
    }

    private static double ConvertMillicoreToUnit(ulong millicores)
    {
        return millicores / 1000.0;
    }
}
