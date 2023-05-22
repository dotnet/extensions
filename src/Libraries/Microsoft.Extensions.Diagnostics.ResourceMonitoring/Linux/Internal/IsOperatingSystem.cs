// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

internal sealed class IsOperatingSystem : IOperatingSystem
{
    public bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
