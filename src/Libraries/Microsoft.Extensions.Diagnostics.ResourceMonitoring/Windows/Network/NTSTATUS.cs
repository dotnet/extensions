// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

/// <summary>
/// Win32 Error Code and NTSTATUS.
/// </summary>
internal enum NTSTATUS : uint
{
    /// <summary>ERROR_SUCCESS.</summary>
    Success = 0x00000000,

    /// <summary>ERROR_INVALID_PARAMETER.</summary>
    InvalidParameter = 0x00000057,

    /// <summary>ERROR_INSUFFICIENT_BUFFER.</summary>
    InsufficientBuffer = 0x0000007A,

    /// <summary>{Operation Failed} The requested operation was unsuccessful.</summary>
    UnsuccessfulStatus = 0xC0000001
}
