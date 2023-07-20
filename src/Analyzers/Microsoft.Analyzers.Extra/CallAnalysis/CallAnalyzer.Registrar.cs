// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

public partial class CallAnalyzer
{
    /// <summary>
    /// Enables call analysis classes to register callbacks.
    /// </summary>
    internal sealed class Registrar
    {
        private readonly State _state;

        internal Registrar(State state, Compilation compilation)
        {
            _state = state;
            Compilation = compilation;
        }

        /// <summary>
        /// Registers a callback to be invoked whenever the given method is invoked directly in code.
        /// </summary>
        /// <remarks>
        /// Note that this is not designed for use with interface methods.
        /// </remarks>
        public void RegisterMethod(IMethodSymbol method, Action<OperationAnalysisContext, IInvocationOperation> action)
        {
            if (!_state.Methods.TryGetValue(method, out var l))
            {
                l = new();
                _state.Methods.Add(method, l);
            }

            l.Add(action);
        }

        /// <summary>
        /// Registers a callback to be invoked whenever the given method overloads are invoked directly in code.
        /// </summary>
        /// <remarks>
        /// Note that this is not designed for use with interface methods.
        /// </remarks>
        public void RegisterMethods(string typeName, string methodName, Action<OperationAnalysisContext, IInvocationOperation> action)
        {
            var dict = new Dictionary<string, string[]>
            {
                { typeName, new[] { methodName } },
            };

            RegisterMethods(dict, action);
        }

        /// <summary>
        /// Registers a callback to be invoked whenever any of the specified methods are invoked.
        /// </summary>
        /// <remarks>
        /// The input dictionary has type names as keys, and arrays of method names as values.
        /// </remarks>
        public void RegisterMethods(Dictionary<string, string[]> methods, Action<OperationAnalysisContext, IInvocationOperation> action)
        {
            foreach (var pair in methods)
            {
                var type = Compilation.GetTypeByMetadataName(pair.Key);
                if (type != null)
                {
                    foreach (var m in pair.Value)
                    {
                        foreach (var method in type.GetMembers(m).OfType<IMethodSymbol>())
                        {
                            RegisterMethod(method, action);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers a callback to be invoked whenever the specified constructor is invoked.
        /// </summary>
        public void RegisterConstructor(IMethodSymbol ctor, Action<OperationAnalysisContext, IObjectCreationOperation> action)
        {
            if (!_state.Ctors.TryGetValue(ctor, out var l))
            {
                l = new();
                _state.Ctors.Add(ctor, l);
            }

            l.Add(action);
        }

        /// <summary>
        /// Registers a callback to be invoked whenever constructors for the given type are invoked.
        /// </summary>
        public void RegisterConstructors(string typeName, Action<OperationAnalysisContext, IObjectCreationOperation> action)
        {
            RegisterConstructors(new[] { typeName }, action);
        }

        /// <summary>
        /// Registers a callback to be invoked whenever constructors for any of the given types are invoked.
        /// </summary>
        public void RegisterConstructors(string[] typeNames, Action<OperationAnalysisContext, IObjectCreationOperation> action)
        {
            foreach (var typeName in typeNames)
            {
                var type = Compilation.GetTypeByMetadataName(typeName);
                if (type != null)
                {
                    foreach (var ctor in type.Constructors)
                    {
                        RegisterConstructor(ctor, action);
                    }
                }
            }
        }

        /// <summary>
        /// Registers a callback to be invoked whenever the given property is invoked (set or get).
        /// </summary>
        public void RegisterProperty(IPropertySymbol prop, Action<OperationAnalysisContext, IPropertyReferenceOperation> action)
        {
            if (!_state.Props.TryGetValue(prop, out var l))
            {
                l = new();
                _state.Props.Add(prop, l);
            }

            l.Add(action);
        }

        /// <summary>
        /// Registers a callback to be invoked whenever any of the given properties are invoked (set or get).
        /// </summary>
        /// <remarks>
        /// The input dictionary has type names as keys, and arrays of method names as values.
        /// </remarks>
        public void RegisterProperties(Dictionary<string, string[]> props, Action<OperationAnalysisContext, IPropertyReferenceOperation> action)
        {
            foreach (var pair in props)
            {
                var type = Compilation.GetTypeByMetadataName(pair.Key);
                if (type != null)
                {
                    foreach (var m in pair.Value)
                    {
                        foreach (var prop in type.GetMembers(m).OfType<IPropertySymbol>())
                        {
                            RegisterProperty(prop, action);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers a callback to be invoked whenever the given interface method is invoked.
        /// </summary>
        public void RegisterInterfaceMethod(IMethodSymbol method, Action<OperationAnalysisContext, IInvocationOperation> action)
        {
            if (!_state.Interfaces.TryGetValue(method.ContainingType, out var handlers))
            {
                handlers = new();
                _state.Interfaces.Add(method.ContainingType, handlers);
            }

            bool found = false;
            foreach (var h in handlers)
            {
                if (SymbolEqualityComparer.Default.Equals(h.Method, method))
                {
                    h.Actions.Add(action);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var h = new MethodHandlers(method);
                h.Actions.Add(action);
                handlers.Add(h);
            }

            _ = _state.InterfaceMethodNames.Add(method.Name);
        }

        /// <summary>
        /// Registers a callback to be invoked whenever any of the given interface methods are invoked.
        /// </summary>
        /// <remarks>
        /// The input dictionary has type names as keys, and arrays of method names as values.
        /// </remarks>
        public void RegisterInterfaceMethods(Dictionary<string, string[]> methods, Action<OperationAnalysisContext, IInvocationOperation> action)
        {
            foreach (var pair in methods)
            {
                var type = Compilation.GetTypeByMetadataName(pair.Key);
                if (type != null)
                {
                    foreach (var m in pair.Value)
                    {
                        foreach (var method in type.GetMembers(m).OfType<IMethodSymbol>())
                        {
                            RegisterInterfaceMethod(method, action);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers a callback to be invoked whenever any of the given exception types are thrown.
        /// </summary>
        public void RegisterExceptionTypes(string[] exceptionTypes, Action<OperationAnalysisContext, IThrowOperation> action)
        {
            foreach (var et in exceptionTypes)
            {
                var type = Compilation.GetTypeByMetadataName(et);
                if (type != null)
                {
                    if (!_state.ExceptionTypes.TryGetValue(type, out var l))
                    {
                        l = new();
                        _state.ExceptionTypes.Add(type, l);
                    }

                    l.Add(action);
                }
            }
        }

        public Compilation Compilation { get; }
    }
}
