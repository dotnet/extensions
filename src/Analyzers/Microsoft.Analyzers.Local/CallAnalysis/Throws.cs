// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.LocalAnalyzers.CallAnalysis;

/// <summary>
/// Recommends using Throw helpers.
/// </summary>
internal sealed class Throws
{
    private static readonly string[] _useThrowsExceptionTypes = new[]
    {
        "System.ArgumentException",
        "System.ArgumentNullException",
        "System.ArgumentOutOfRangeException",
    };

    public Throws(CallAnalyzer.Registrar reg)
    {
        reg.RegisterExceptionTypes(_useThrowsExceptionTypes, Handle);

        static void Handle(OperationAnalysisContext context, IThrowOperation op)
        {
            var convOp = (IConversionOperation?)op.Exception;
            var creationOp = (IObjectCreationOperation?)convOp?.Operand;

            if (creationOp?.Type != null)
            {
                if (op.Syntax.IsKind(SyntaxKind.ThrowStatement))
                {
                    var diagnostic = Diagnostic.Create(
                        DiagDescriptors.ThrowsStatement,
                        op.Syntax.GetLocation(),
                        $"Microsoft.Shared.Diagnostics.Throws.{creationOp.Type.Name}");

                    context.ReportDiagnostic(diagnostic);
                }
                else if (op.Syntax.IsKind(SyntaxKind.ThrowExpression))
                {
                    if (creationOp.Type.Name == "ArgumentNullException")
                    {
                        var throwExpression = (ThrowExpressionSyntax)op.Syntax;
                        if (throwExpression.Parent is BinaryExpressionSyntax binaryExpression)
                        {
                            var diagnostic = Diagnostic.Create(
                                DiagDescriptors.ThrowsExpression,
                                binaryExpression.GetLocation(),
                                "Microsoft.Shared.Diagnostics.Throws.IfNull");

                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
