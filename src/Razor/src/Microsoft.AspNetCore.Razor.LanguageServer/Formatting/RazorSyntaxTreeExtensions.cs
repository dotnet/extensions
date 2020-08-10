// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal static class RazorSyntaxTreeExtensions
    {
        public static IReadOnlyList<FormattingSpan> GetFormattingSpans(this RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var visitor = new FormattingVisitor(syntaxTree.Source);
            visitor.Visit(syntaxTree.Root);

            return visitor.FormattingSpans;
        }

        public static IReadOnlyList<RazorDirectiveSyntax> GetCodeBlockDirectives(this RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            // We want all nodes of type RazorDirectiveSyntax which will contain code.
            // Since code block directives occur at the top-level, we don't need to dive deeper into unrelated nodes.
            var codeBlockDirectives = syntaxTree.Root
                .DescendantNodes(node => node is RazorDocumentSyntax || node is MarkupBlockSyntax || node is CSharpCodeBlockSyntax)
                .OfType<RazorDirectiveSyntax>()
                .Where(directive => directive.DirectiveDescriptor?.Kind == DirectiveKind.CodeBlock)
                .ToList();

            return codeBlockDirectives;
        }

        public static IReadOnlyList<CSharpStatementSyntax> GetCSharpStatements(this RazorSyntaxTree syntaxTree)
        {
            if (syntaxTree is null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            // We want all nodes that represent Razor C# statements, @{ ... }.
            var statements = syntaxTree.Root.DescendantNodes().OfType<CSharpStatementSyntax>().ToList();
            return statements;
        }
    }
}
