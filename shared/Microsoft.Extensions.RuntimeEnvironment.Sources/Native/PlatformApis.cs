// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Internal.Native
{
    internal static class PlatformApis
    {
        public static string GetOSVersion()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return NativeMethods.Windows.RtlGetVersion() ?? string.Empty;
            }
            throw new PlatformNotSupportedException();
        }
    }
}