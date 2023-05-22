// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;

/// <summary>
/// Recommends using R9 composite text formatting functionality.
/// </summary>
internal sealed class StringFormat
{
    public StringFormat(CallAnalyzer.Registrar reg)
    {
        foreach (var method in reg.Compilation.GetSpecialType(SpecialType.System_String).GetMembers("Format").OfType<IMethodSymbol>())
        {
            reg.RegisterMethod(method, Handle);
        }

        var type = reg.Compilation.GetTypeByMetadataName("System.Text.StringBuilder");
        if (type != null)
        {
            foreach (var method in type.GetMembers("AppendFormat").OfType<IMethodSymbol>())
            {
                reg.RegisterMethod(method, Handle);
            }
        }

        static void Handle(OperationAnalysisContext context, IInvocationOperation op)
        {
            var format = GetFormatArgument(op);
            if (format.ChildNodes().First().IsKind(SyntaxKind.StringLiteralExpression))
            {
                var properties = new Dictionary<string, string?>();
                if (op.TargetMethod.Name == "Format")
                {
                    properties.Add("StringFormat", null);
                }

                var diagnostic = Diagnostic.Create(DiagDescriptors.StringFormat, op.Syntax.GetLocation(), properties.ToImmutableDictionary());
                context.ReportDiagnostic(diagnostic);
            }

            static SyntaxNode GetFormatArgument(IInvocationOperation invocation)
            {
                var sm = invocation.SemanticModel!;
                var arguments = invocation.Arguments;
                var typeInfo = sm.GetTypeInfo(arguments[0].Syntax.ChildNodes().First());

                // This check is needed to identify exactly which argument of string.Format is the format argument
                // The format might be passed as first or second argument
                // if there are more than 1 arguments and first argument is IFormatProvider then format is second argument otherwise it is first
                if (arguments.Length > 1 && typeInfo.Type != null && typeInfo.Type.AllInterfaces.Any(i => i.MetadataName == "IFormatProvider"))
                {
                    return arguments[1].Syntax;
                }

                return arguments[0].Syntax;
            }
        }
    }
}
