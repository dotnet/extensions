// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// C# analyzer that recommends removing superfluous uses of ?.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConditionalAccessAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagDescriptors.ConditionalAccess);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // only report diagnostics on .NET 6 or above since previous runtimes don't have good enough nullability annotations
            if (compilationStartContext.Compilation.IsNet6OrGreater())
            {
                var maybeNull = compilationStartContext.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.MaybeNullAttribute");

                compilationStartContext.RegisterOperationAction(operationAnalysisContext =>
                {
                    var op = (IConditionalAccessOperation)operationAnalysisContext.Operation;

                    ITypeSymbol? type;
                    switch (op.Operation.Kind)
                    {
                        case OperationKind.PropertyReference:
                        {
                            var propRef = (IPropertyReferenceOperation)op.Operation;
                            if (MaybeNull(propRef.Property.GetAttributes()))
                            {
                                // property can be null, independent of its type signature
                                return;
                            }

                            type = propRef.Property.Type;
                            break;
                        }

                        case OperationKind.FieldReference:
                        {
                            var fieldRef = (IFieldReferenceOperation)op.Operation;
                            if (MaybeNull(fieldRef.Field.GetAttributes()))
                            {
                                return;
                            }

                            type = fieldRef.Field.Type;
                            break;
                        }

                        case OperationKind.Invocation:
                        {
                            var invocation = (IInvocationOperation)op.Operation;
                            if (MaybeNull(invocation.TargetMethod.GetReturnTypeAttributes()))
                            {
                                return;
                            }

                            type = op.Operation.Type;
                            break;
                        }

                        default:
                        {
                            type = op.Operation.Type;
                            break;
                        }
                    }

                    if (type != null)
                    {
                        if (type is ITypeParameterSymbol tp)
                        {
                            if (!tp.HasNotNullConstraint)
                            {
                                // a generic type without a notnull constraint can potentially hold null values
                                return;
                            }
                        }

                        // if the type of the operand is not nullable, then we have a candidate
                        if (type.NullableAnnotation == NullableAnnotation.NotAnnotated)
                        {
                            // if the operand is a parameter on a public method or interface method, then don't report it
                            if (op.Operation.Kind == OperationKind.ParameterReference)
                            {
                                var pr = (IParameterReferenceOperation)op.Operation;
                                var method = pr.Parameter.ContainingSymbol as IMethodSymbol;

                                if (pr.Parameter.ContainingSymbol.IsExternallyVisible()
                                    || (method != null && method.ImplementsPublicInterface()))
                                {
                                    // this is a ? applied to a parameter of a public method, let it slide...
                                    return;
                                }
                            }

                            var diagnostic = Diagnostic.Create(DiagDescriptors.ConditionalAccess, op.Syntax.GetLocation());
                            operationAnalysisContext.ReportDiagnostic(diagnostic);
                        }
                    }
                }, OperationKind.ConditionalAccess);

                bool MaybeNull(ImmutableArray<AttributeData> attrs)
                {
                    foreach (var attr in attrs)
                    {
                        if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, maybeNull))
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        });
    }
}
