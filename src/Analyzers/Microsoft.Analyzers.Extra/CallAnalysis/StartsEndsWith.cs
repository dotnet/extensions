// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends char (instead of string) versions of String.StartsWith and String.EndsWith when possible.
/// </summary>
internal sealed class StartsEndsWith
{
    public StartsEndsWith(CallAnalyzer.Registrar reg)
    {
        var stringType = reg.Compilation.GetSpecialType(SpecialType.System_String);
        var stringCompType = reg.Compilation.GetTypeByMetadataName("System.StringComparison");

        var startsWith = stringType.GetMembers("StartsWith").OfType<IMethodSymbol>()
            .Where(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, stringType))
            .Where(m =>
                (m.Parameters.Length == 1) ||
                (m.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(m.Parameters[1].Type, stringCompType)));

        var endsWith = stringType.GetMembers("EndsWith").OfType<IMethodSymbol>()
            .Where(m => SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, stringType))
            .Where(m =>
                (m.Parameters.Length == 1) ||
                (m.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(m.Parameters[1].Type, stringCompType)));

        foreach (var m in startsWith)
        {
            reg.RegisterMethod(m, Handle);
        }

        foreach (var m in endsWith)
        {
            reg.RegisterMethod(m, Handle);
        }

        static void Handle(OperationAnalysisContext context, IInvocationOperation op)
        {
            var s = op.Arguments[0].Value.ConstantValue.Value as string;

            if (s != null && s.Length == 1)
            {
                if (op.Arguments.Length > 1 && op.Arguments[1].Value.ConstantValue.HasValue)
                {
                    var comp = (StringComparison)op.Arguments[1].Value.ConstantValue.Value!;
                    if (comp != StringComparison.Ordinal)
                    {
                        return;
                    }
                }

                var diagnostic = Diagnostic.Create(DiagDescriptors.StartsEndsWith, op.Syntax.GetLocation(), op.TargetMethod.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
