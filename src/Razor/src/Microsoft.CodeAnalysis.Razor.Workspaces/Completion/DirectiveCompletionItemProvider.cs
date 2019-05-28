// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    [Shared]
    [Export(typeof(RazorCompletionItemProvider))]
    internal class DirectiveCompletionItemProvider : RazorCompletionItemProvider
    {
        private static readonly IEnumerable<DirectiveDescriptor> DefaultDirectives = new[]
        {
            CSharpCodeParser.AddTagHelperDirectiveDescriptor,
            CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
            CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
        };

        public override IReadOnlyList<RazorCompletionItem> GetCompletionItems(RazorSyntaxTree syntaxTree, TagHelperDocumentContext tagHelperDocumentContext, SourceSpan location)
        {
            if (syntaxTree is null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var completions = new List<RazorCompletionItem>();
            if (AtDirectiveCompletionPoint(syntaxTree, location))
            {
                var directiveCompletions = GetDirectiveCompletionItems(syntaxTree);
                completions.AddRange(directiveCompletions);
            }

            return completions;
        }

        // Internal for testing
        internal static bool AtDirectiveCompletionPoint(RazorSyntaxTree syntaxTree, SourceSpan location)
        {
            if (syntaxTree == null)
            {
                return false;
            }

            var change = new SourceChange(location, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                return false;
            }

            // Do not provide IntelliSense for explicit expressions. Explicit expressions will usually look like:
            // [@] [(] [DateTime.Now] [)]
            var implicitExpression = owner.FirstAncestorOrSelf<CSharpImplicitExpressionSyntax>();
            if (implicitExpression == null)
            {
                return false;
            }

            if (owner.ChildNodes().Any(n => !n.IsToken || !IsDirectiveCompletableToken((AspNetCore.Razor.Language.Syntax.SyntaxToken)n)))
            {
                // Implicit expression contains invalid directive tokens
                return false;
            }

            if (implicitExpression.FirstAncestorOrSelf<RazorDirectiveSyntax>() != null)
            {
                // Implicit expression is nested in a directive
                return false;
            }

            if (implicitExpression.FirstAncestorOrSelf<CSharpStatementSyntax>() != null)
            {
                // Implicit expression is nested in a statement
                return false;
            }

            if (implicitExpression.FirstAncestorOrSelf<MarkupElementSyntax>() != null)
            {
                // Implicit expression is nested in an HTML element
                return false;
            }

            if (implicitExpression.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>() != null)
            {
                // Implicit expression is nested in a TagHelper
                return false;
            }

            return true;
        }

        // Internal for testing
        internal static List<RazorCompletionItem> GetDirectiveCompletionItems(RazorSyntaxTree syntaxTree)
        {
            var defaultDirectives = FileKinds.IsComponent(syntaxTree.Options.FileKind) ? Array.Empty<DirectiveDescriptor>() : DefaultDirectives;
            var directives = syntaxTree.Options.Directives.Concat(defaultDirectives);
            var completionItems = new List<RazorCompletionItem>();
            foreach (var directive in directives)
            {
                var completionDisplayText = directive.DisplayName ?? directive.Directive;
                var completionItem = new RazorCompletionItem(
                    completionDisplayText,
                    directive.Directive,
                    RazorCompletionItemKind.Directive);
                var completionDescription = new DirectiveCompletionDescription(directive.Description);
                completionItem.SetDirectiveCompletionDescription(completionDescription);
                completionItems.Add(completionItem);
            }

            return completionItems;
        }

        // Internal for testing
        internal static bool IsDirectiveCompletableToken(AspNetCore.Razor.Language.Syntax.SyntaxToken token)
        {
            return token.Kind == SyntaxKind.Identifier ||
                // Marker symbol
                token.Kind == SyntaxKind.Marker;
        }
    }
}
