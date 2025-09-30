// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// Provides resource quota information for resource monitoring purposes.
/// </summary>
/// <remarks>
/// This interface defines a contract for retrieving resource quotas, which include
/// memory and CPU limits and requests that may be imposed by container orchestrators
/// or resource management systems.
/// </remarks>
public interface IResourceQuotaProvider
{
    /// <summary>
    /// Gets the current resource quota containing memory and CPU limits and requests.Returned <see cref="ResourceQuota"/> is used in resource monitoring calculations.
    /// </summary>
    /// <returns>
    /// A <see cref="ResourceQuota"/> instance containing the current resource constraints
    /// including memory limits, CPU limits, memory requests, and CPU requests.
    /// </returns>
    ResourceQuota GetResourceQuota();
}

