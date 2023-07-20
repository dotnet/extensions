// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends using value tuples, instead of reference tuples.
/// </summary>
internal sealed class ValueTuple
{
    private readonly string[] _tupleTypes = new[]
    {
        "System.Tuple`1",
        "System.Tuple`2",
        "System.Tuple`3",
        "System.Tuple`4",
        "System.Tuple`5",
        "System.Tuple`6",
        "System.Tuple`7",
        "System.Tuple`8",
    };

    public ValueTuple(CallAnalyzer.Registrar reg)
    {
        var type = reg.Compilation.GetTypeByMetadataName("System.Tuple");
        if (type != null)
        {
            foreach (var method in type.GetMembers("Create").OfType<IMethodSymbol>())
            {
                reg.RegisterMethod(method, HandleMethod);
            }

            reg.RegisterConstructors(_tupleTypes, HandleConstructor);
        }

        static void HandleMethod(OperationAnalysisContext context, IInvocationOperation op)
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.ValueTuple, op.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        static void HandleConstructor(OperationAnalysisContext context, IObjectCreationOperation op)
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.ValueTuple, op.Syntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
