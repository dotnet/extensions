// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

/// <summary>
/// Class/struct/record declaration syntax receiver for generators.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal sealed class TypeDeclarationSyntaxReceiver : ISyntaxReceiver
{
    internal static ISyntaxReceiver Create() => new TypeDeclarationSyntaxReceiver();

    /// <summary>
    /// Gets class/struct/record declaration syntax holders after visiting nodes.
    /// </summary>
    public ICollection<TypeDeclarationSyntax> TypeDeclarations { get; } = new List<TypeDeclarationSyntax>();

    /// <inheritdoc/>
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classSyntax)
        {
            TypeDeclarations.Add(classSyntax);
        }
        else if (syntaxNode is StructDeclarationSyntax structSyntax)
        {
            TypeDeclarations.Add(structSyntax);
        }
        else if (syntaxNode is RecordDeclarationSyntax recordSyntax)
        {
            TypeDeclarations.Add(recordSyntax);
        }
        else if (syntaxNode is InterfaceDeclarationSyntax interfaceSyntax)
        {
            TypeDeclarations.Add(interfaceSyntax);
        }
    }
}
