// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

internal class LinuxResourceQuotasProvider : IResourceQuotasProvider

{
    public ResourceQuotas GetResourceQuotas()
    {
        // bring logic from LinuxUtilizationProvider for limits and requests
    }
}

