// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Gen.EnumStrings;

[Generator]
public class EnumStringsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<SyntaxNode> typeDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SymbolLoader.EnumStringsAttribute,
                (_, _) => true,
                (context, _) => context.TargetNode);

        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypes =
            context.CompilationProvider.Combine(typeDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => HandleAnnotatedNodes(source.Item1, source.Item2, spc));
    }

    private static void HandleAnnotatedNodes(Compilation compilation, ImmutableArray<SyntaxNode> nodes, SourceProductionContext context)
    {
        if (!SymbolLoader.TryLoad(compilation, out var symbolHolder))
        {
            // Not eligible compilation
            return;
        }

        var parser = new Parser(compilation, context.ReportDiagnostic, symbolHolder!, context.CancellationToken);

        var toStringMethods = parser.GetToStringMethods(nodes);
        if (toStringMethods.Count > 0)
        {
            var emitter = new Emitter();
            var result = emitter.Emit(toStringMethods, symbolHolder!.FreezerSymbol != null, context.CancellationToken);

            context.AddSource("EnumStrings.g.cs", SourceText.From(result, Encoding.UTF8));
        }
    }
}
