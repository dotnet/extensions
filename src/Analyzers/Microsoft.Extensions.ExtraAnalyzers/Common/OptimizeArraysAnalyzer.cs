// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers;

/// <summary>
/// C# analyzer that recommends using Array.Empty, or making arrays of literals into static readonly fields.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OptimizeArraysAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagDescriptors.MakeArrayStatic);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(operationAnalysisContext =>
        {
            var arrayCreation = (IArrayCreationOperation)operationAnalysisContext.Operation;

            if (arrayCreation.Syntax.AncestorsAndSelf().Any(x => x.IsKind(SyntaxKind.Attribute)))
            {
                return;
            }

            var initializer = arrayCreation.Initializer;
            var target = arrayCreation.Parent;
            var type = ((IArrayTypeSymbol?)arrayCreation.Type)?.ElementType;

            var empty = initializer?.ElementValues.Length == 0;
#pragma warning disable S1067 // Expressions should not be too complex
            if (initializer == null
                && arrayCreation.DimensionSizes.Length == 1
                && arrayCreation.DimensionSizes[0] is ILiteralOperation lit
                && lit.ConstantValue.HasValue
                && lit.ConstantValue.Value is 0)
            {
                empty = true;
            }
#pragma warning restore S1067 // Expressions should not be too complex

            if (empty)
            {
                // empty arrays, handled by CA1825
                return;
            }

            if (initializer == null)
            {
                return;
            }

            foreach (var value in initializer.ElementValues)
            {
                if (value.Kind == OperationKind.Literal)
                {
                    continue;
                }

                if (value.Kind == OperationKind.FieldReference)
                {
                    var fieldRef = (IFieldReferenceOperation)value;
                    if (fieldRef.ConstantValue.HasValue)
                    {
                        continue;
                    }
                }

                return;
            }

            // found a candidate array initialization...

            if (target != null)
            {
                if (InitializesStaticFieldOrProp(target))
                {
                    return;
                }
            }

            var diagnostic = Diagnostic.Create(DiagDescriptors.MakeArrayStatic, arrayCreation.Syntax.GetLocation());
            operationAnalysisContext.ReportDiagnostic(diagnostic);
        }, OperationKind.ArrayCreation);
    }

    private static bool InitializesStaticFieldOrProp(IOperation op)
    {
        // if this array allocation is done to initialize a static field or property, then don't report it
        switch (op.Kind)
        {
            case OperationKind.FieldInitializer:
            {
                var fieldRef = (IFieldInitializerOperation)op;
                foreach (var field in fieldRef.InitializedFields)
                {
                    if (field.IsStatic)
                    {
                        return true;
                    }
                }

                break;
            }

            case OperationKind.PropertyInitializer:
            {
                var propRef = (IPropertyInitializerOperation)op;
                foreach (var prop in propRef.InitializedProperties)
                {
                    if (prop.IsStatic)
                    {
                        return true;
                    }
                }

                break;
            }

            case OperationKind.Conversion:
            case OperationKind.Argument:
            case OperationKind.Invocation:
            {
                if (op.Parent != null)
                {
                    return InitializesStaticFieldOrProp(op.Parent);
                }

                break;
            }
        }

        return false;
    }
}
