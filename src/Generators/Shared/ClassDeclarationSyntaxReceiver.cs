// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#pragma warning disable CA1716
namespace Microsoft.Gen.Shared;
#pragma warning restore CA1716

/// <summary>
/// Class declaration syntax receiver for generators.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal sealed class ClassDeclarationSyntaxReceiver : ISyntaxReceiver
{
    internal static ISyntaxReceiver Create() => new ClassDeclarationSyntaxReceiver();

    /// <summary>
    /// Gets class declaration syntax holders after visiting nodes.
    /// </summary>
    public ICollection<ClassDeclarationSyntax> ClassDeclarations { get; } = new List<ClassDeclarationSyntax>();

    /// <inheritdoc/>
    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is ClassDeclarationSyntax classSyntax)
        {
            ClassDeclarations.Add(classSyntax);
        }
    }
}
