// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.LocalAnalyzers.CallAnalysis;

/// <summary>
/// Recommends using the shared ToInvariantString extension method.
/// </summary>
internal sealed class ToInvariantString
{
    private static readonly SpecialType[] _intTypes = new[]
    {
        SpecialType.System_Byte,
        SpecialType.System_Int16,
        SpecialType.System_Int32,
        SpecialType.System_Int64,
    };

    public ToInvariantString(CallAnalyzer.Registrar reg)
    {
        var formatProvider = reg.Compilation.GetTypeByMetadataName("System.IFormatProvider");

        foreach (var type in _intTypes)
        {
            foreach (var method in reg.Compilation.GetSpecialType(type).GetMembers("ToString").OfType<IMethodSymbol>())
            {
                if (method.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, formatProvider))
                {
                    reg.RegisterMethod(method, Handle);
                }
            }
        }

        static void Handle(OperationAnalysisContext context, IInvocationOperation op)
        {
            var a = op.Arguments[0];
            if (a.Value is IConversionOperation conv)
            {
                if (conv.Operand is IPropertyReferenceOperation prop)
                {
                    var cultureInfo = context.Compilation.GetTypeByMetadataName("System.Globalization.CultureInfo");
                    var invariantCulture = cultureInfo?.GetMembers("InvariantCulture").OfType<IPropertySymbol>().SingleOrDefault();

                    if (SymbolEqualityComparer.Default.Equals(invariantCulture, prop.Property))
                    {
                        var diagnostic = Diagnostic.Create(DiagDescriptors.ToInvariantString, op.Syntax.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
