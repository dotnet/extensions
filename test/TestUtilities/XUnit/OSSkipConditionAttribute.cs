// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Borrowed from https://github.com/dotnet/aspnetcore/blob/95ed45c67/src/Testing/src/xunit/

using System;
#if NETCOREAPP || NET471_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace Microsoft.TestUtilities;

#pragma warning disable CA1019 // Define accessors for attribute arguments
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class OSSkipConditionAttribute : Attribute, ITestCondition
{
    private readonly OperatingSystems _excludedOperatingSystem;
    private readonly OperatingSystems _osPlatform;

    public OSSkipConditionAttribute(OperatingSystems operatingSystem)
        : this(operatingSystem, GetCurrentOS())
    {
    }

    // to enable unit testing
    internal OSSkipConditionAttribute(OperatingSystems operatingSystem, OperatingSystems osPlatform)
    {
        _excludedOperatingSystem = operatingSystem;
        _osPlatform = osPlatform;
    }

    public bool IsMet
    {
        get
        {
            var skip = (_excludedOperatingSystem & _osPlatform) == _osPlatform;

            // Since a test would be executed only if 'IsMet' is true, return false if we want to skip
            return !skip;
        }
    }

    public string SkipReason { get; set; } = "Test cannot run on this operating system.";

    private static OperatingSystems GetCurrentOS()
    {
#if NETCOREAPP || NET471_OR_GREATER
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return OperatingSystems.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return OperatingSystems.Linux;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return OperatingSystems.MacOSX;
        }

        throw new PlatformNotSupportedException();
#else
        // RuntimeInformation API is only avaialble in .NET Framework 4.7.1+
        // .NET Framework 4.7 and below can only run on Windows.
        return OperatingSystems.Windows;
#endif
    }
}
#pragma warning restore CA1019 // Define accessors for attribute arguments
