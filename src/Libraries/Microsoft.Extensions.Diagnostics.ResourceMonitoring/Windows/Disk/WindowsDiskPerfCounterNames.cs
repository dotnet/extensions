// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Disk;

internal static class WindowsDiskPerfCounterNames
{
    internal const string DiskWriteBytesCounter = "Disk Write Bytes/sec";
    internal const string DiskReadBytesCounter = "Disk Read Bytes/sec";
    internal const string DiskWritesCounter = "Disk Writes/sec";
    internal const string DiskReadsCounter = "Disk Reads/sec";
    internal const string DiskIdleTimeCounter = "% Idle Time";
}
