// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

internal class LinuxResourceQuotaProvider : IResourceQuotaProvider
{
    private readonly ILinuxUtilizationParser _parser;
    private bool _useLinuxCalculationV2;

    public LinuxResourceQuotaProvider(ILinuxUtilizationParser parser, IOptions<ResourceMonitoringOptions> options)
    {
        _parser = parser;
        _useLinuxCalculationV2 = options.Value.UseLinuxCalculationV2;
    }

    public ResourceQuota GetResourceQuota()
    {
        var resourceQuota = new ResourceQuota();
        if (_useLinuxCalculationV2)
        {
            resourceQuota.LimitsCpu = _parser.GetCgroupLimitV2();
            resourceQuota.RequestsCpu = _parser.GetCgroupRequestCpuV2();
        }
        else
        {
            resourceQuota.LimitsCpu = _parser.GetCgroupLimitedCpus();
            resourceQuota.RequestsCpu = _parser.GetCgroupRequestCpu();
        }

        resourceQuota.LimitsMemory = _parser.GetAvailableMemoryInBytes();
        resourceQuota.RequestsMemory = resourceQuota.LimitsMemory;

        return resourceQuota;
    }
}

