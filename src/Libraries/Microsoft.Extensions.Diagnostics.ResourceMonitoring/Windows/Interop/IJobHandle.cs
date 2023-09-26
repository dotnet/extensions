// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using static Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop.JobObjectInfo;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

/// <summary>
/// An interface to enable the mocking of job information retrieval.
/// </summary>
internal interface IJobHandle : IDisposable
{
    /// <summary>
    /// Get the current CPU rate information from the Job object.
    /// </summary>
    /// <returns>CPU rate information.</returns>
    JOBOBJECT_CPU_RATE_CONTROL_INFORMATION GetJobCpuLimitInfo();

    /// <summary>
    /// Get the current CPU rate information from the Job object.
    /// </summary>
    /// <returns>CPU rate information.</returns>
    JOBOBJECT_BASIC_ACCOUNTING_INFORMATION GetBasicAccountingInfo();

    /// <summary>
    /// Get the extended limit information from the Job object.
    /// </summary>
    /// <returns>Extended limit information.</returns>
    JOBOBJECT_EXTENDED_LIMIT_INFORMATION GetExtendedLimitInfo();
}
