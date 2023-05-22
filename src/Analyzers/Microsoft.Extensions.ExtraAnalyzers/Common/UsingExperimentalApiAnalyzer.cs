// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Extensions.ExtraAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UsingExperimentalApiAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagDescriptors.UsingExperimentalApi);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            context.RegisterSyntaxNodeAction(context =>
            {
                var sn = (IdentifierNameSyntax)context.Node;
                if (sn.IsVar)
                {
                    return;
                }

                var sym = context.SemanticModel.GetSymbolInfo(sn).Symbol;

                if (sym != null && HasExperimentalAttribute(sym))
                {
                    var diagnostic = Diagnostic.Create(DiagDescriptors.UsingExperimentalApi, sn.GetLocation(), sym.Name);
                    context.ReportDiagnostic(diagnostic);
                }
                else if (sym is INamedTypeSymbol type && HasExperimentalAttribute(type.ContainingAssembly))
                {
                    var diagnostic = Diagnostic.Create(DiagDescriptors.UsingExperimentalApi, sn.GetLocation(), type.ContainingAssembly.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }, SyntaxKind.IdentifierName);

            static bool HasExperimentalAttribute(ISymbol sym)
            {
                foreach (var attributeData in sym.GetAttributes())
                {
                    if (attributeData.AttributeClass?.Name == "ExperimentalAttribute")
                    {
                        var ns = attributeData.AttributeClass.ContainingNamespace.ToString();
                        if (ns is "System.Diagnostics.CodeAnalysis" or "Microsoft.Extensions.Diagnostics")
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        });
    }
}
