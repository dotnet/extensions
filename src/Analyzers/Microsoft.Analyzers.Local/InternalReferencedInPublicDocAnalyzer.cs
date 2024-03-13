// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.LocalAnalyzers.Utilities;

namespace Microsoft.Extensions.LocalAnalyzers;

/// <summary>
/// C# analyzer that warns about referencing internal symbols in public xml documentation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InternalReferencedInPublicDocAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(DiagDescriptors.InternalReferencedInPublicDoc);

    private static MemberDeclarationSyntax? FindDocumentedSymbol(XmlCrefAttributeSyntax crefNode)
    {
        // Find the documentation comment the cref node is part of
        var documentationComment = crefNode.Ancestors(ascendOutOfTrivia: false).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();
        if (documentationComment == null)
        {
            return null;
        }

        // Find documented symbol simply as first parent that is a declaration
        // If the comment is not placed above any declaration, this takes the enclosing declaration
        var symbolNode = crefNode.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
        if (symbolNode == null)
        {
            return null;
        }

        // To filter out the cases when enclosing declaration is taken,
        // make sure that the comment of found symbol is the same as the comment of cref being analyzed
        var symbolComment = symbolNode.GetLeadingTrivia()
            .Select(trivia => trivia.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();
        if (symbolComment != documentationComment)
        {
            return null;
        }

        return symbolNode;
    }

    private static bool IsNodeExternallyVisible(MemberDeclarationSyntax memberNode)
    {
        // In a way, the code replicates SymbolExtensions.IsExternallyVisible on syntax tree level
        // It traverses up to namespace declaration and checks if all levels are externally visible
        MemberDeclarationSyntax? node = memberNode;
        while (node != null && !IsNamespace(node))
        {
            bool isPublic = false;
            bool isProtected = false;
            bool isPrivate = false;
            bool hasModifiers = false;
            foreach (var modifier in node.Modifiers)
            {
                switch (modifier.Text)
                {
                    case "public":
                        isPublic = true;
                        break;
                    case "protected":
                        isProtected = true;
                        break;
                    case "private":
                        isPrivate = true;
                        break;
                }

                hasModifiers = true;
            }

            if (!hasModifiers // no modifiers => internal, not visible
                || isPrivate // private and private protected are both not visible
                || (!isPublic && !isProtected) // public and protected are only other externally visible options
               )
            {
                return false;
            }

            node = node.Parent as MemberDeclarationSyntax;
        }

        return true;

        static bool IsNamespace(MemberDeclarationSyntax n) =>
            n is BaseNamespaceDeclarationSyntax;
    }

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        _ = context ?? throw new ArgumentNullException(nameof(context));

        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(ValidateCref, SyntaxKind.XmlCrefAttribute);
    }

    private void ValidateCref(SyntaxNodeAnalysisContext context)
    {
        var crefNode = (XmlCrefAttributeSyntax)context.Node;
        if (crefNode.IsMissing)
        {
            return;
        }

        var symbolNode = FindDocumentedSymbol(crefNode);
        if (symbolNode == null)
        {
            return;
        }

        // Only externally visible symbols should be considered
        // Sometimes (for fields and events) the symbol is unknown
        // In such a case, use nodes instead of symbols
        var symbol = context.SemanticModel.GetDeclaredSymbol(symbolNode);
        var isExternallyVisible = symbol?.IsExternallyVisible() ?? IsNodeExternallyVisible(symbolNode);
        if (!isExternallyVisible)
        {
            return;
        }

        var referencedName = crefNode.Cref.ToString();
        if (string.IsNullOrWhiteSpace(referencedName))
        {
            return;
        }

        // Find what the cref attribute references; only successful binding is considered now, candidates aren't analyzed
        var referencedSymbol = context.SemanticModel.GetSymbolInfo(crefNode.Cref).Symbol;
        if (referencedSymbol == null)
        {
            return;
        }

        // Report referencing a not externally visible symbol
        if (!referencedSymbol.IsExternallyVisible())
        {
            var diagnostic = Diagnostic.Create(DiagDescriptors.InternalReferencedInPublicDoc, crefNode.Cref.GetLocation(), referencedName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
