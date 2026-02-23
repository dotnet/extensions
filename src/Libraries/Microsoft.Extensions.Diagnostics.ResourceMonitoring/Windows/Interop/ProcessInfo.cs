// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

[ExcludeFromCodeCoverage]
internal sealed class ProcessInfo : IProcessInfo
{
    public ulong GetMemoryUsage()
    {
        ulong memoryUsage = 0;
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                memoryUsage += (ulong)process.WorkingSet64;
            }
            catch
            {
                // Ignore various exceptions including, but not limited:
                // AccessDenied (from kernel processes),
                // InvalidOperation (process does not exist anymore)
                // and silently continue to the next process.
            }
            finally
            {
                process?.Dispose();
            }
        }

        return memoryUsage;
    }

    public ulong GetCurrentProcessMemoryUsage()
    {
        return (ulong)Environment.WorkingSet;
    }
}
