// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP2_0 || NET461
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DiagnosticAdapter.Infrastructure;

namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    // To diagnose issues with the emitted code, define GENERATE_ASSEMBLIES and call Save() in the
    // immediate window after defining the problematic method.
    //
    // The use peverify or another tool to look at the generated code.
    public static class ProxyMethodEmitter
    {
#if GENERATE_ASSEMBLIES
        private static volatile int Counter = 0;

        private static readonly AssemblyBuilder AssemblyBuilder;
        private static readonly ModuleBuilder ModuleBuilder;

        static ProxyMethodEmitter()
        {
            var name = new AssemblyName("Microsoft.Extensions.DiagnosticAdapter.ProxyMethodAssembly");
            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(name.Name + ".dll");
        }
#endif

        private static readonly MethodInfo ProxyFactoryGenericMethod =
            typeof(IProxyFactory).GetTypeInfo().GetDeclaredMethod(nameof(IProxyFactory.CreateProxy));

        public static Func<object, object, IProxyFactory, bool> CreateProxyMethod(MethodInfo method, Type inputType)
        {
            var name = string.Format("Proxy_Method_From_{0}_To_{1}", inputType.Name, method);

            // Define the method-adapter as Func<'listener', 'data', 'proxy factory', bool>, we'll do casts inside.
            var dynamicMethod = new DynamicMethod(
                name,
                returnType: typeof(bool),
                parameterTypes: new Type[] { typeof(object), typeof(object), typeof(IProxyFactory) },
                restrictedSkipVisibility: true);

            var parameters = method.GetParameters();
            var mappings = GetPropertyToParameterMappings(inputType, parameters);
            EmitMethod(dynamicMethod.GetILGenerator(), inputType, mappings, method, parameters);

#if GENERATE_ASSEMBLIES
            AddToAssembly(inputType, mappings, method, parameters);
#endif
            var @delegate = dynamicMethod.CreateDelegate(typeof(Func<object, object, IProxyFactory, bool>));
            return (Func<object, object, IProxyFactory, bool>)@delegate;
        }

        private static PropertyInfo[] GetPropertyToParameterMappings(Type inputType, ParameterInfo[] parameters)
        {
            var properties = inputType.GetTypeInfo().DeclaredProperties.ToArray();
            var mappings = new PropertyInfo[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                for (var j = 0; j < properties.Length; j++)
                {
                    var property = properties[j];
                    if (string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (mappings[i] == null)
                        {
                            mappings[i] = property;
                        }
                        else
                        {
                            // If the mapping is not-null, we've already found a property matching this parameter name.
                            // This is an ambiguity that must be caused by properties with different casings.
                            throw new InvalidOperationException(
                                Resources.FormatConverter_TypeMustNotHavePropertiesThatVaryByCase(
                                    inputType.FullName,
                                    property.Name.ToLowerInvariant())); // ToLower for testability
                        }
                    }
                }
            }

            return mappings;
        }

        private static void EmitMethod(
            ILGenerator il,
            Type inputType,
            PropertyInfo[] mappings,
            MethodInfo method,
            ParameterInfo[] parameters)
        {
            // Define a local for each method parameters. This is needed when the parameter is
            // a value type, but we'll do it for all for simplicity.
            for (var i = 0; i < parameters.Length; i++)
            {
                il.DeclareLocal(parameters[i].ParameterType);
            }

            var endLabel = il.DefineLabel(); // Marks the 'return'
            var happyPathLabel = il.DefineLabel(); // Marks the 'happy path' - wherein we dispatch to the listener.

            //// Check if the input value is of the type we can handle.
            il.Emit(OpCodes.Ldarg_1); // The event-data
            il.Emit(OpCodes.Isinst, inputType);
            il.Emit(OpCodes.Brtrue, happyPathLabel);

            // We get here if the event-data doesn't match the type we can handle.
            //
            // Push 'false' onto the stack and return.
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Br, endLabel);

            // We get here if the event-data matches the type we can handle.
            il.MarkLabel(happyPathLabel);

            // Initialize locals to hold each parameter value.
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.ParameterType.GetTypeInfo().IsValueType)
                {
                    // default-initialize each value type.
                    il.Emit(OpCodes.Ldloca_S, i);
                    il.Emit(OpCodes.Initobj, parameter.ParameterType);
                }
                else
                {
                    // null-initialize each reference type.
                    il.Emit(OpCodes.Ldnull);
                    il.Emit(OpCodes.Stloc_S, i);
                }
            }

            // Evaluate all properties and store them in the locals.
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var mapping = mappings[i];
                if (mapping != null)
                {
                    // No proxy required, just load the value.
                    if (parameter.ParameterType.GetTypeInfo().IsAssignableFrom(mapping.PropertyType.GetTypeInfo()))
                    {
                        il.Emit(OpCodes.Ldarg_1); // The event-data
                        il.Emit(OpCodes.Castclass, inputType);
                        il.Emit(OpCodes.Callvirt, mapping.GetMethod);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg_2); // The proxy-factory

                        il.Emit(OpCodes.Ldarg_1); // The event-data
                        il.Emit(OpCodes.Castclass, inputType);
                        il.Emit(OpCodes.Callvirt, mapping.GetMethod);

                        // If we have a value type box it to the target type.
                        if (mapping.PropertyType.GetTypeInfo().IsValueType)
                        {
                            il.Emit(OpCodes.Box, mapping.PropertyType);
                        }

                        var factoryMethod = ProxyFactoryGenericMethod.MakeGenericMethod(parameter.ParameterType);
                        il.Emit(OpCodes.Callvirt, factoryMethod);
                    }

                    il.Emit(OpCodes.Stloc_S, i);
                }
            }

            // Set up the call to the listener.
            //
            // Push the listener object, and then all of the argument values.
            il.Emit(OpCodes.Ldarg_0); // The listener
            il.Emit(OpCodes.Castclass, method.DeclaringType);

            // Push arguments onto the stack
            for (var i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldloc_S, i);
            }

            // Call the method in the listener
            il.Emit(OpCodes.Callvirt, method);

            // Success!
            //
            // Push 'true' onto the stack and return.
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Br, endLabel);

            // We expect that whoever branched to here put a boolean value (I4_0, I4_1) on top of the stack.
            il.MarkLabel(endLabel);
            il.Emit(OpCodes.Ret);
        }

#if GENERATE_ASSEMBLIES
        private static void AddToAssembly(
            Type inputType,
            PropertyInfo[] mappings,
            MethodInfo method,
            ParameterInfo[] parameters)
        {
            var typeName = $"Type_For_Proxy_Method_From_{inputType.Name}_To_{method}_{Counter++}";

            var typeBuilder = ModuleBuilder.DefineType(typeName, TypeAttributes.Abstract);
            var methodBuilder = typeBuilder.DefineMethod(
                "Proxy",
                MethodAttributes.Public,
                CallingConventions.Standard,
                returnType: typeof(bool),
                parameterTypes: new Type[] { typeof(object), typeof(object), typeof(IProxyFactory) });

            var il = methodBuilder.GetILGenerator();
            EmitMethod(il, inputType, mappings, method, parameters);

            typeBuilder.CreateType();
        }

        public static string Save()
        {
            ProxyAssembly.Save();

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
