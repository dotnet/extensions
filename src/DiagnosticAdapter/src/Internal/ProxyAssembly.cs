// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP2_0 || NET461
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    // To diagnose issues with the emitted code, define GENERATE_ASSEMBLIES and call Save() in the
    // immediate window after defining the problematic method.
    //
    // The use peverify or another tool to look at the generated code.
    public static class ProxyAssembly
    {
        private static readonly object Lock;
        private static int Counter;

        private static AssemblyBuilder AssemblyBuilder;
        private static ModuleBuilder ModuleBuilder;

        static ProxyAssembly()
        {
            Lock = new object();

            var assemblyName = new AssemblyName("Microsoft.Extensions.DiagnosticAdapter.ProxyAssembly");
#if GENERATE_ASSEMBLIES
            var access = AssemblyBuilderAccess.RunAndSave;
#else
            var access = AssemblyBuilderAccess.Run;
#endif

            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule("Microsoft.Extensions.DiagnosticAdapter.ProxyAssembly.dll");
        }

        public static TypeBuilder DefineType(
            string name,
            TypeAttributes attributes,
            Type baseType,
            Type[] interfaces)
        {
            lock (Lock)
            {
                name = name + "_" + Counter++;
                return ModuleBuilder.DefineType(name, attributes, baseType, interfaces);
            }
        }

#if GENERATE_ASSEMBLIES
        public static string Save()
        {
            AssemblyBuilder.Save(ModuleBuilder.ScopeName);
            return ModuleBuilder.FullyQualifiedName;
        }
#endif
    }
}
#elif NETSTANDARD2_0
#else
#error Target frameworks should be updated
#endif
