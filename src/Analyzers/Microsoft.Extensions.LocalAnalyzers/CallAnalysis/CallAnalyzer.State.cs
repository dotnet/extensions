// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.LocalAnalyzers.CallAnalysis;

public partial class CallAnalyzer
{
    internal sealed class State
    {
        public readonly Dictionary<IMethodSymbol, List<Action<OperationAnalysisContext, IInvocationOperation>>> Methods = new(SymbolEqualityComparer.Default);
        public readonly Dictionary<IMethodSymbol, List<Action<OperationAnalysisContext, IObjectCreationOperation>>> Ctors = new(SymbolEqualityComparer.Default);
        public readonly Dictionary<IPropertySymbol, List<Action<OperationAnalysisContext, IPropertyReferenceOperation>>> Props = new(SymbolEqualityComparer.Default);
        public readonly Dictionary<ITypeSymbol, List<Action<OperationAnalysisContext, IThrowOperation>>> ExceptionTypes = new(SymbolEqualityComparer.Default);
        public readonly Dictionary<ITypeSymbol, List<MethodHandlers>> Interfaces = new(SymbolEqualityComparer.Default);
        public readonly HashSet<string> InterfaceMethodNames = new();
    }

    internal sealed class MethodHandlers
    {
        public MethodHandlers(IMethodSymbol method)
        {
            Method = method;
        }

        public IMethodSymbol Method { get; }
        public List<Action<OperationAnalysisContext, IInvocationOperation>> Actions { get; } = new();
    }
}
