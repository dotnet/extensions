// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Gen.Logging.Parsing;

namespace Microsoft.Gen.Logging;

[Generator]
public class LoggingGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                Parsing.SymbolLoader.LogMethodAttribute,
                (syntaxNode, _) => syntaxNode.Parent is TypeDeclarationSyntax,
                (context, _) => (TypeDeclarationSyntax)context.TargetNode.Parent!);

        IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndTypes =
            context.CompilationProvider.Combine(typeDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => HandleAnnotatedTypes(source.Item1, source.Item2, spc));
    }

    private static void HandleAnnotatedTypes(Compilation compilation, IEnumerable<TypeDeclarationSyntax> types, SourceProductionContext context)
    {
        var p = new Parsing.Parser(compilation, context.ReportDiagnostic, context.CancellationToken);

        var logTypes = p.GetLogTypes(types.Distinct());
        if (logTypes.Count > 0)
        {
            var e = new Emission.Emitter();
            var result = e.Emit(logTypes, context.CancellationToken);

            context.AddSource("Logging.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
}
