// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

internal class LinuxResourceQuotaProvider : ResourceQuotaProvider
{
    private readonly ILinuxUtilizationParser _parser;
    private bool _useLinuxCalculationV2;

    public LinuxResourceQuotaProvider(ILinuxUtilizationParser parser, IOptions<ResourceMonitoringOptions> options)
    {
        _parser = parser;
        _useLinuxCalculationV2 = options.Value.UseLinuxCalculationV2;
    }

    public override ResourceQuota GetResourceQuota()
    {
        var resourceQuota = new ResourceQuota();
        if (_useLinuxCalculationV2)
        {
            resourceQuota.MaxCpuInCores = _parser.GetCgroupLimitV2();
            resourceQuota.BaselineCpuInCores = _parser.GetCgroupRequestCpuV2();
        }
        else
        {
            resourceQuota.MaxCpuInCores = _parser.GetCgroupLimitedCpus();
            resourceQuota.BaselineCpuInCores = _parser.GetCgroupRequestCpu();
        }

        resourceQuota.MaxMemoryInBytes = _parser.GetAvailableMemoryInBytes();
        resourceQuota.BaselineMemoryInBytes = resourceQuota.MaxMemoryInBytes; // TODO: use real value

        return resourceQuota;
    }
}

