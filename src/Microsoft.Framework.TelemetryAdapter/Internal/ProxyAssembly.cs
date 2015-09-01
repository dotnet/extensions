// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if PROXY_SUPPORT

using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Framework.TelemetryAdapter.Internal
{
    public static class ProxyAssembly
    {
        private static volatile int Counter = 0;

        private static AssemblyBuilder AssemblyBuilder;
        private static ModuleBuilder ModuleBuilder;

        static ProxyAssembly()
        {
            var assemblyName = new AssemblyName("Microsoft.Framework.TelemetryAdapter.ProxyAssembly");
            var access = AssemblyBuilderAccess.Run;

            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule("Microsoft.Framework.TelemetryAdapter.ProxyAssembly.dll");
        }

        public static TypeBuilder DefineType(
            string name,
            TypeAttributes attributes,
            Type baseType,
            Type[] interfaces)
        {
            name = name + "_" + Counter++;
            return ModuleBuilder.DefineType(name, attributes, baseType, interfaces);
        }
    }
}
#endif