// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Interop;

[ExcludeFromCodeCoverage]
internal sealed class ProcessInfo : IProcessInfo
{
    public ulong GetMemoryUsage()
    {
        ulong memoryUsage = 0;
        var processes = Process.GetProcesses();
        foreach (var process in processes)
        {
            try
            {
                memoryUsage += (ulong)process.WorkingSet64;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // Ignore various exceptions including, but not limited:
                // AccessDenied (from kernel processes),
                // InvalidOperation (process does not exist anymore)
                // and silently continue to the next process.
            }
            finally
            {
#pragma warning disable EA0011 // Consider removing unnecessary conditional access operator (?)
                process?.Dispose();
#pragma warning restore EA0011 // Consider removing unnecessary conditional access operator (?)
            }
        }

        return memoryUsage;
    }

    public ulong GetCurrentProcessMemoryUsage()
    {
        using Process process = Process.GetCurrentProcess();
        return (ulong)process.WorkingSet64;
    }
}
