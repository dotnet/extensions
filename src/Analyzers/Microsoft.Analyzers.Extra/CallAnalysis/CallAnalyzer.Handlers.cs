// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

public partial class CallAnalyzer
{
    private sealed class Handlers
    {
        private readonly State _state;

        public Handlers(State state)
        {
            _state = state;
        }

        public void HandleInvocation(OperationAnalysisContext context)
        {
            var op = (IInvocationOperation)context.Operation;
            var target = op.TargetMethod;

            if (target != null)
            {
                if (_state.Methods.TryGetValue(target.OriginalDefinition, out var handlers))
                {
                    if (op.Arguments.Length == target.Parameters.Length)
                    {
                        foreach (var handler in handlers)
                        {
                            handler(context, op);
                        }
                    }
                }

                if (_state.InterfaceMethodNames.Contains(target.Name))
                {
                    var type = target.ContainingType;
                    if (type.TypeKind == TypeKind.Interface)
                    {
                        if (_state.Interfaces.TryGetValue(type, out var l))
                        {
                            foreach (var h in l)
                            {
                                if (SymbolEqualityComparer.Default.Equals(target, h.Method))
                                {
                                    foreach (var action in h.Actions)
                                    {
                                        action(context, op);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var iface in type.AllInterfaces)
                        {
                            if (_state.Interfaces.TryGetValue(iface, out var l))
                            {
                                foreach (var h in l)
                                {
                                    var impl = type.FindImplementationForInterfaceMember(h.Method);
                                    if (SymbolEqualityComparer.Default.Equals(target, impl))
                                    {
                                        foreach (var action in h.Actions)
                                        {
                                            action(context, op);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void HandleObjectCreation(OperationAnalysisContext context)
        {
            var op = (IObjectCreationOperation)context.Operation;
            if (op.Constructor != null)
            {
                if (_state.Ctors.TryGetValue(op.Constructor.OriginalDefinition, out var handlers))
                {
                    if (op.Arguments.Length == op.Constructor.Parameters.Length)
                    {
                        foreach (var handler in handlers)
                        {
                            handler(context, op);
                        }
                    }
                }
            }
        }

        public void HandlePropertyReference(OperationAnalysisContext context)
        {
            var op = (IPropertyReferenceOperation)context.Operation;
            if (_state.Props.TryGetValue(op.Property, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler(context, op);
                }
            }
        }
    }
}
