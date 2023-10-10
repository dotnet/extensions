// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Gen.ContextualOptions.Model;

internal sealed class OptionsContextType
{
    public readonly List<Diagnostic> Diagnostics = [];
    public readonly INamedTypeSymbol Symbol;
    public readonly ImmutableArray<TypeDeclarationSyntax> Definitions;
    public readonly ImmutableArray<string> OptionsContextProperties;
    public string Keyword => Definitions[0].Keyword.Text;
    public string? Namespace => Symbol.ContainingNamespace.IsGlobalNamespace ? null : Symbol.ContainingNamespace.ToString();
    public string Name => Symbol.Name;

    public bool ShouldEmit => Diagnostics.TrueForAll(diag => diag.Severity != DiagnosticSeverity.Error);

    public string HintName => $"{Namespace}.{Name}";

    public OptionsContextType(
        INamedTypeSymbol symbol,
        ImmutableArray<TypeDeclarationSyntax> definitions,
        ImmutableArray<string> optionsContextProperties)
    {
        Symbol = symbol;
        Definitions = definitions;
        OptionsContextProperties = optionsContextProperties;
    }
}
