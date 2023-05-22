// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// Mocking running OS to be able to test library on windows machine.
/// </summary>
internal interface IOperatingSystem
{
    /// <summary>
    /// Gets a value indicating whether the program is running on Linux.
    /// </summary>
    bool IsLinux { get; }
}
