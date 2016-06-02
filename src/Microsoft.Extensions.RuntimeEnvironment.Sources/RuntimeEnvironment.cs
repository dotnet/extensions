// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Internal
{
    internal static class RuntimeEnvironment
    {
        static RuntimeEnvironment()
        {
            OperatingSystemVersion = Native.PlatformApis.GetOSVersion();

            RuntimeType = GetRuntimeType();
        }

        /// <summary>
        /// Don't use in shipping packages
        /// </summary>
        static public string OperatingSystemVersion { get; }

        static public string RuntimeType { get; }

        static private string GetRuntimeType()
        {
#if NET451
            return Type.GetType("Mono.Runtime") != null ? "Mono" : "CLR";
#else
            return "CoreCLR";
#endif
        }
    }
}