// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

internal sealed class ProcessInfoWrapper : IProcessInfo
{
    [ExcludeFromCodeCoverage]
    public ProcessInfo.APP_MEMORY_INFORMATION GetCurrentAppMemoryInfo()
    {
        return ProcessInfo.GetCurrentAppMemoryInfo();
    }
}
