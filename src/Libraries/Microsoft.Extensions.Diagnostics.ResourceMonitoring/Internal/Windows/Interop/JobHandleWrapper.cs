﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal.JobObjectInfo;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// Wrapper class for the <see cref="JobObjectInfo.SafeJobHandle"/> class.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class JobHandleWrapper : IJobHandle
{
    private readonly JobObjectInfo.SafeJobHandle _winJobHandle;

    public JobHandleWrapper()
    {
        _winJobHandle = new JobObjectInfo.SafeJobHandle();
    }

    public void Dispose()
    {
        _winJobHandle.Dispose();
    }

    public JOBOBJECT_CPU_RATE_CONTROL_INFORMATION GetJobCpuLimitInfo()
    {
        return _winJobHandle.GetJobCpuLimitInfo();
    }

    public JOBOBJECT_BASIC_ACCOUNTING_INFORMATION GetBasicAccountingInfo()
    {
        return _winJobHandle.GetBasicAccountingInfo();
    }

    public JOBOBJECT_EXTENDED_LIMIT_INFORMATION GetExtendedLimitInfo()
    {
        return _winJobHandle.GetExtendedLimitInfo();
    }
}
