// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ROSLYN_4_0_OR_GREATER

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Gen.ContextualOptions.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.ContextualOptions;

[Generator]
[ExcludeFromCodeCoverage]
public class Generator : IIncrementalGenerator
{
    private static readonly HashSet<string> _attributeNames = new()
    {
        "Microsoft.Extensions.Options.Contextual.OptionsContextAttribute",
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        GeneratorUtilities.Initialize(context, _attributeNames, HandleAnnotatedTypes);
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

#else

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.ContextualOptions.Model;

namespace Microsoft.Gen.ContextualOptions;

/// <summary>
/// Generates options context implementations for user annotated objects.
/// </summary>
[Generator]
[ExcludeFromCodeCoverage]
public class Generator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) =>
        context.RegisterForSyntaxNotifications(() => new ContextReceiver(context.CancellationToken));

    public void Execute(GeneratorExecutionContext context)
    {
        var receiver = context.SyntaxReceiver as ContextReceiver;
        if (receiver is null || !receiver.TryGetTypeDeclarations(context.Compilation, out var typeDeclarations))
        {
            // nothing to do yet
            return;
        }

        var list = new List<OptionsContextType>();
        foreach (var type in Parser.GetContextualOptionTypes(typeDeclarations!))
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

#endif
