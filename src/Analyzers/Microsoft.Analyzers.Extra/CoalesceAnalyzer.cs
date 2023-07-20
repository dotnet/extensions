// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// C# analyzer that recommends removing superfluous uses of ?? and ??=.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CoalesceAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagDescriptors.CoalesceAssignment, DiagDescriptors.Coalesce);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // only report diagnostics on .NET 6, since previous runtimes don't have good enough nullability annotations
            if (compilationStartContext.Compilation.IsNet6OrGreater())
            {
                compilationStartContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    var op = (ICoalesceAssignmentOperation)operationAnalysisContext.Operation;

                    var type = op.Target.Type;
                    if (type != null
                        && type.NullableAnnotation == NullableAnnotation.NotAnnotated
                        && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                    {
                        if (op.Target.Kind == OperationKind.ParameterReference)
                        {
                            var pr = (IParameterReferenceOperation)op.Target;
                            var method = pr.Parameter.ContainingSymbol as IMethodSymbol;

                            if (pr.Parameter.ContainingSymbol.IsExternallyVisible()
                                || (method != null && method.ImplementsPublicInterface()))
                            {
                                // this is a ??= applied to a parameter of a public method, let it slide...
                                return;
                            }
                        }

                        var diagnostic = Diagnostic.Create(DiagDescriptors.CoalesceAssignment, op.Syntax.GetLocation());
                        operationAnalysisContext.ReportDiagnostic(diagnostic);
                    }
                }, OperationKind.CoalesceAssignment);

                compilationStartContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    var op = (ICoalesceOperation)operationAnalysisContext.Operation;

                    var type = op.Value.Type;
                    if (type != null
                        && type.NullableAnnotation == NullableAnnotation.NotAnnotated
                        && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                    {
                        if (op.Value.Kind == OperationKind.ParameterReference)
                        {
                            var pr = (IParameterReferenceOperation)op.Value;
                            var method = pr.Parameter.ContainingSymbol as IMethodSymbol;

                            if (pr.Parameter.ContainingSymbol.IsExternallyVisible()
                                || (method != null && method.ImplementsPublicInterface()))
                            {
                                // this is a ?? applied to a parameter of a public method, let it slide...
                                return;
                            }
                        }

                        var diagnostic = Diagnostic.Create(DiagDescriptors.Coalesce, op.Syntax.GetLocation());
                        operationAnalysisContext.ReportDiagnostic(diagnostic);
                    }
                }, OperationKind.Coalesce);
            }
        });
    }
}
