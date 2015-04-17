// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Framework.Internal
{
    internal static class NativeLibraryLoader
    {
        private const uint LOAD_WITH_ALTERED_SEARCH_PATH = 8;

        [DllImport("api-ms-win-core-libraryloader-l1-1-0")]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("api-ms-win-core-libraryloader-l1-1-0")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public static bool TryLoad(string dllName)
        {
            string applicationBase;
#if NET45 || NET451 || NET452 || DNX451 || DNX452
            applicationBase = AppDomain.CurrentDomain.BaseDirectory;
#else
            applicationBase = AppContext.BaseDirectory;
#endif

            return TryLoad(dllName, applicationBase);
        }

        public static bool TryLoad(string dllName, string applicationBase)
        {
            if (dllName == null)
            {
                throw new ArgumentNullException(nameof(dllName));
            }
            if (applicationBase == null)
            {
                throw new ArgumentNullException(nameof(applicationBase));
            }
            if (IsLoaded(dllName))
            {
                return true;
            }

            // TODO: Use GetSystemInfo (in api-ms-win-core-sysinfo-l1-1-0.dll) to detect ARM
            var architecture = IntPtr.Size == 4 ? "x86" : "x64";

            if (!dllName.EndsWith(".dll"))
            {
                dllName += ".dll";
            }

            var dllPath = Path.Combine(applicationBase, architecture, dllName);

            if (!File.Exists(dllPath))
            {
                return false;
            }

            return LoadLibraryEx(dllPath, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH) != IntPtr.Zero;
        }

        public static bool IsLoaded(string dllName) => GetModuleHandle(dllName) != IntPtr.Zero;
    }
}
