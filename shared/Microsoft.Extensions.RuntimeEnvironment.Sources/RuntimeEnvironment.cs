// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Internal
{
    internal static class RuntimeEnvironment
    {
        private static readonly Lazy<string> _operatingSystemVersion = new Lazy<string>(() => Native.PlatformApis.GetOSVersion());

        /// <summary>
        /// Don't use in shipping packages
        /// </summary>
        static public string OperatingSystemVersion => _operatingSystemVersion.Value;

        static public string RuntimeType { get; } = GetRuntimeType();

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