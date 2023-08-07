// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsingToStringInLoggersAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagDescriptors.UsingToStringInLoggers);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            compilationStartContext.RegisterOperationBlockStartAction(operationBlockContext =>
            {
                if (operationBlockContext.OwningSymbol.Kind != SymbolKind.Method)
                {
                    return;
                }

                operationBlockContext.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            });
        });
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (IsLoggerMethod(invocation.TargetMethod))
        {
            foreach (var diagnostic in AnalyzeLogger(invocation))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private static bool IsLoggerMethod(ISymbol symbol)
    {
        return symbol.GetAttributes().Any(a => a.AttributeClass != null && IsLoggerMessageAttribute(a.AttributeClass));
    }

    private static bool IsLoggerMessageAttribute(ISymbol attributeSymbol)
    {
        return attributeSymbol.Name == "LoggerMessageAttribute"
            && attributeSymbol.ContainingNamespace.ToString() == "Microsoft.Extensions.Logging";
    }

    private static IEnumerable<Diagnostic> AnalyzeLogger(IInvocationOperation invocation)
    {
        foreach (var arg in invocation.Arguments)
        {
            if (arg.Value is IInvocationOperation argOperation
                && argOperation.TargetMethod.Name == "ToString")
            {
                yield return Diagnostic.Create(DiagDescriptors.UsingToStringInLoggers, arg.Syntax.GetLocation());
            }
        }
    }
}
