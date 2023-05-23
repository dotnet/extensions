// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.ExtraAnalyzers.Utilities;

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
            var experimentalAttribute = context.Compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.ExperimentalAttribute");

            context.RegisterSyntaxNodeAction(context =>
            {
                var sn = (IdentifierNameSyntax)context.Node;
                if (sn.IsVar)
                {
                    return;
                }

                var sym = context.SemanticModel.GetSymbolInfo(sn).Symbol;

                if (sym != null
                    && (sym.Kind is not SymbolKind.Namespace and not SymbolKind.Label and not SymbolKind.Discard)
                    && sym.IsContaminated(experimentalAttribute))
                {
                    if (sym.IsExternallyVisible())
                    {
                        context.ReportDiagnostic(Diagnostic.Create(DiagDescriptors.UsingExperimentalApi, sn.GetLocation(), sym));
                    }
                }
            }, SyntaxKind.IdentifierName);
        });
    }
}
