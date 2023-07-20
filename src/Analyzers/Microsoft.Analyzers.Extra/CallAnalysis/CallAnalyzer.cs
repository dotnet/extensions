// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Composite analyzer that efficiently inspects various types of method/ctor/property calls.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class CallAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        DiagDescriptors.StartsEndsWith,
        DiagDescriptors.LegacyLogging,
        DiagDescriptors.StaticTime,
        DiagDescriptors.EnumStrings,
        DiagDescriptors.ValueTuple,
        DiagDescriptors.Arrays,
        DiagDescriptors.LegacyCollection,
        DiagDescriptors.Split);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            var state = new State();

            var reg = new Registrar(state, compilationStartContext.Compilation);

            _ = new Arrays(reg);
            _ = new EnumStrings(reg);
            _ = new LegacyLogging(reg);
            _ = new StartsEndsWith(reg);
            _ = new StaticTime(reg);
            _ = new ValueTuple(reg);
            _ = new LegacyCollection(reg);
            _ = new Split(reg);

            var handlers = new Handlers(state);
            compilationStartContext.RegisterOperationAction(handlers.HandleInvocation, OperationKind.Invocation);
            compilationStartContext.RegisterOperationAction(handlers.HandleObjectCreation, OperationKind.ObjectCreation);
            compilationStartContext.RegisterOperationAction(handlers.HandlePropertyReference, OperationKind.PropertyReference);
        });
    }
}
