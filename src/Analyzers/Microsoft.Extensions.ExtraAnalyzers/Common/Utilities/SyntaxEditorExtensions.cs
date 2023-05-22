// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.Extensions.ExtraAnalyzers.Utilities;

/// <summary>
/// Class contains <see cref="SyntaxEditor"/> extensions.
/// </summary>
internal static class SyntaxEditorExtensions
{
    /// <summary>
    /// Tries to add using directive.
    /// </summary>
    /// <param name="editor">The syntax editor.</param>
    /// <param name="namespaceName">The namespace name.</param>
    public static void TryAddUsingDirective(this SyntaxEditor editor, NameSyntax namespaceName)
    {
        if (editor.GetChangedRoot() is CompilationUnitSyntax documentRoot)
        {
            var anyUsings = documentRoot.Usings.Any(u => u.Name.GetText().ToString().Equals(namespaceName.ToString(), StringComparison.Ordinal));
            var usingDirective = SyntaxFactory.UsingDirective(namespaceName);
            documentRoot = anyUsings ? documentRoot : documentRoot.AddUsings(usingDirective).WithAdditionalAnnotations(Formatter.Annotation);
            editor.ReplaceNode(editor.OriginalRoot, documentRoot);
        }
    }
}
