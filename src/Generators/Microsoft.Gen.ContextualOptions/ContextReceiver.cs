// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Gen.ContextualOptions;

/// <summary>
/// Type declaration syntax receiver for generators.
/// </summary>
internal sealed class ContextReceiver : ISyntaxReceiver
{
    private readonly CancellationToken _token;

    public ContextReceiver(CancellationToken token)
    {
        _token = token;
    }

    private readonly List<TypeDeclarationSyntax> _typeDeclarations = [];

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        _token.ThrowIfCancellationRequested();

        if (syntaxNode is TypeDeclarationSyntax type
            && type is not InterfaceDeclarationSyntax)
        {
            _typeDeclarations.Add(type);
        }
    }

    public bool TryGetTypeDeclarations(Compilation compilation, out Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>>? typeDeclarations)
    {
        if (!SymbolLoader.TryLoad(compilation, out var holder))
        {
            typeDeclarations = default;
            return false;
        }

        typeDeclarations = _typeDeclarations
            .ToLookup(declaration => declaration.SyntaxTree)
            .SelectMany(declarations => declarations.Select(declaration => (symbol: compilation.GetSemanticModel(declarations.Key).GetDeclaredSymbol(declaration), declaration)))
            .Where(_ => _.symbol is INamedTypeSymbol)
            .Where(_ => _.symbol!.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, holder!.OptionsContextAttribute)))
            .ToLookup(_ => _.symbol, _ => _.declaration, comparer: SymbolEqualityComparer.Default)
            .ToDictionary<IGrouping<ISymbol?, TypeDeclarationSyntax>, INamedTypeSymbol, List<TypeDeclarationSyntax>>(
                group => (INamedTypeSymbol)group.Key!, group => group.ToList(), comparer: SymbolEqualityComparer.Default);

        return true;
    }
}
