// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Gen.ContextualOptions.Model;

namespace Microsoft.Gen.ContextualOptions;

[Generator]
public class ContextualOptionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<SyntaxNode> typeDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Microsoft.Extensions.Options.Contextual.OptionsContextAttribute",
                (_, _) => true,
                (context, _) => context.TargetNode);

        IncrementalValueProvider<(Compilation, ImmutableArray<SyntaxNode>)> compilationAndTypes =
            context.CompilationProvider.Combine(typeDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => HandleAnnotatedTypes(source.Item1, source.Item2, spc));
    }

    private static void HandleAnnotatedTypes(Compilation compilation, IEnumerable<SyntaxNode> nodes, SourceProductionContext context)
    {
        if (!SymbolLoader.TryLoad(compilation, out var holder))
        {
            return;
        }

        var typeDeclarations = nodes.OfType<TypeDeclarationSyntax>()
            .ToLookup(declaration => declaration.SyntaxTree)
            .SelectMany(declarations => declarations.Select(declaration => (symbol: compilation.GetSemanticModel(declarations.Key).GetDeclaredSymbol(declaration), declaration)))
            .Where(_ => _.symbol is INamedTypeSymbol)
            .Where(_ => _.symbol!.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, holder!.OptionsContextAttribute)))
            .ToLookup(_ => _.symbol, _ => _.declaration, comparer: SymbolEqualityComparer.Default)
            .ToDictionary<IGrouping<ISymbol?, TypeDeclarationSyntax>, INamedTypeSymbol, List<TypeDeclarationSyntax>>(
                group => (INamedTypeSymbol)group.Key!, group => group.ToList(), comparer: SymbolEqualityComparer.Default);

        var list = new List<OptionsContextType>();
        foreach (var type in Parser.GetContextualOptionTypes(typeDeclarations))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            type.Diagnostics.ForEach(context.ReportDiagnostic);

            if (type.ShouldEmit)
            {
                list.Add(type);
            }
        }

        if (list.Count > 0)
        {
            var emitter = new Emitter();
            context.AddSource($"ContextualOptions.g.cs", emitter.Emit(list.OrderBy(x => x.Namespace + "." + x.Name)));
        }
    }
}
