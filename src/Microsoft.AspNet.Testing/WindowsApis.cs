// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNXCORE50

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNet.Testing
{
    internal static class WindowsApis
    {
        public static Version OSVersion
        {
            get
            {
                uint dwVersion = GetVersion();

                int major = (int)(dwVersion & 0xFF);
                int minor = (int)((dwVersion >> 8) & 0xFF);

                return new Version(major, minor);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetVersion();
    }
}

#endif
