// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Extensions.LocalAnalyzers.CallAnalysis;

/// <summary>
/// Composite analyzer that efficiently inspects various types of method/ctor/property calls.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class CallAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagDescriptors.ToInvariantString,
        DiagDescriptors.ThrowsStatement,
        DiagDescriptors.ThrowsExpression);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            var state = new State();

            var reg = new Registrar(state, compilationStartContext.Compilation);

            _ = new ToInvariantString(reg);
            _ = new Throws(reg);

            var handlers = new Handlers(state);
#pragma warning disable R9A044
            compilationStartContext.RegisterOperationAction(handlers.HandleInvocation, OperationKind.Invocation);
            compilationStartContext.RegisterOperationAction(handlers.HandleObjectCreation, OperationKind.ObjectCreation);
            compilationStartContext.RegisterOperationAction(handlers.HandlePropertyReference, OperationKind.PropertyReference);
            compilationStartContext.RegisterOperationAction(handlers.HandleThrow, OperationKind.Throw);
#pragma warning restore R9A044
        });
    }
}
