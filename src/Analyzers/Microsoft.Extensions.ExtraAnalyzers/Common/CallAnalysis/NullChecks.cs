// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends removing null checks from internal-facing functions.
/// </summary>
internal sealed class NullChecks
{
    private static readonly Dictionary<string, string[]> _nullCheckMethods = new()
    {
        ["Microsoft.Extensions.Diagnostics.Throws"] = new[]
        {
            "IfNull",
        },
    };

    public NullChecks(CallAnalyzer.Registrar reg)
    {
        reg.RegisterMethods(_nullCheckMethods, HandleMethod);
        reg.RegisterExceptionTypes(new[] { "System.ArgumentNullException" }, HandleException);

        static void HandleMethod(OperationAnalysisContext context, IInvocationOperation op) => HandleNullCheck(context, op);

        static void HandleException(OperationAnalysisContext context, IThrowOperation op) => HandleNullCheck(context, op);

        static void HandleNullCheck(OperationAnalysisContext context, IOperation op)
        {
            var method = op.SemanticModel?.GetEnclosingSymbol(op.Syntax.GetLocation().SourceSpan.Start) as IMethodSymbol;
            if (method != null)
            {
                // externally visible methods can have null checks
                if (method.IsExternallyVisible())
                {
                    return;
                }

                // see if the method implements any part of any public interface
                if (method.ImplementsPublicInterface())
                {
                    return;
                }

                var diagnostic = Diagnostic.Create(DiagDescriptors.NullCheck, op.Syntax.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
