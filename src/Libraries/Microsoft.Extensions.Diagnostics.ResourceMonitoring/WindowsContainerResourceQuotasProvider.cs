// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal class WindowsContainerResourceQuotasProvider : IResourceQuotasProvider
{
    public ResourceQuotas GetResourceQuotas()
    {
        // bring logic from WindowsContainerSnapshotProvider for limits and requests
    }
}

