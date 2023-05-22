// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends switch to faster alternatives to Enum.GetName and Enum.ToString.
/// </summary>
internal sealed class EnumStrings
{
    public EnumStrings(CallAnalyzer.Registrar reg)
    {
        reg.RegisterMethods("System.Enum", "GetName", HandleGetName);
        reg.RegisterMethods("System.Enum", "ToString", HandleToString);

        static void HandleToString(OperationAnalysisContext context, IInvocationOperation op)
        {
            var inst = op.Instance;
            if (inst != null && inst.Kind == OperationKind.FieldReference)
            {
                var fieldRef = (IFieldReferenceOperation)inst;
                if (fieldRef.Field.Type.TypeKind == TypeKind.Enum)
                {
                    var d = Diagnostic.Create(DiagDescriptors.EnumStrings, op.Syntax.GetLocation(), "'nameof'", "Enum.ToString");
                    context.ReportDiagnostic(d);
                    return;
                }
            }

            var diagnostic = Diagnostic.Create(DiagDescriptors.EnumStrings, op.Syntax.GetLocation(), "the '[EnumStrings]' code generator", "Enum.ToString");
            context.ReportDiagnostic(diagnostic);
        }

        static void HandleGetName(OperationAnalysisContext context, IInvocationOperation op)
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.EnumStrings, op.Syntax.GetLocation(), "the '[EnumStrings] code generator", "Enum.GetName");
            context.ReportDiagnostic(diagnostic);
        }
    }
}
