// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    internal class MarkupTransitionCompletionItemProvider : RazorCompletionItemProvider
    {
        private static readonly IReadOnlyCollection<string> ElementCommitCharacters = new HashSet<string>{ ">" };

        private readonly HtmlFactsService _htmlFactsService;

        private static RazorCompletionItem _markupTransitionCompletionItem;
        public static RazorCompletionItem MarkupTransitionCompletionItem
        {
            get
            {
                if (_markupTransitionCompletionItem == null)
                {
                    var completionDisplayText = SyntaxConstants.TextTagName;
                    _markupTransitionCompletionItem = new RazorCompletionItem(
                        completionDisplayText,
                        completionDisplayText,
                        RazorCompletionItemKind.MarkupTransition,
                        ElementCommitCharacters);
                    var completionDescription = new MarkupTransitionCompletionDescription(Resources.MarkupTransition_Description);
                    _markupTransitionCompletionItem.SetMarkupTransitionCompletionDescription(completionDescription);
                }

                return _markupTransitionCompletionItem;
            }
        }

        public MarkupTransitionCompletionItemProvider(HtmlFactsService htmlFactsService)
        {
            if (htmlFactsService is null)
            {
                throw new ArgumentNullException(nameof(htmlFactsService));
            }

            _htmlFactsService = htmlFactsService;
        }

        public override IReadOnlyList<RazorCompletionItem> GetCompletionItems(RazorSyntaxTree syntaxTree, TagHelperDocumentContext tagHelperDocumentContext, SourceSpan location)
        {
            if (syntaxTree is null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            var change = new SourceChange(location, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                Debug.Fail("Owner should never be null.");
                return Array.Empty<RazorCompletionItem>();
            }

            if (!AtMarkupTransitionCompletionPoint(owner))
            {
                return Array.Empty<RazorCompletionItem>();
            }

            var parent = owner.Parent;

            // Also helps filter out edge cases like `< te` and `< te=""`
            // (see comment in AtMarkupTransitionCompletionPoint)
            if (!_htmlFactsService.TryGetElementInfo(parent, out var containingTagNameToken, out var attributes) ||
                !containingTagNameToken.Span.IntersectsWith(location.AbsoluteIndex))
            {
                return Array.Empty<RazorCompletionItem>();
            }

            var completions = new List<RazorCompletionItem>() { MarkupTransitionCompletionItem };
            return completions;
        }

        // Internal for testing
        internal static bool AtMarkupTransitionCompletionPoint(RazorSyntaxNode owner)
        {
            /* Only provide IntelliSense for C# code blocks, of the form:
                @{ }, @code{ }, @functions{ }, @if(true){ }

               Note for the `< te` and `< te=""` cases:
               The cases are not handled by AtMarkupTransitionCompletionPoint but
               rather by the HtmlFactsService which purposely prohibits the completion
               when it's unable to extract the tag contents. This ensures we aren't
               providing incorrect completion in the above two syntactically invalid
               scenarios.
            */
            var encapsulatingMarkupElementNodeSeen = false;

            foreach (var ancestor in owner.Ancestors()) {
                if (ancestor is MarkupElementSyntax markupNode)
                {
                    if (encapsulatingMarkupElementNodeSeen) {
                        return false;
                    }

                    encapsulatingMarkupElementNodeSeen = true;
                }

                if (ancestor is CSharpCodeBlockSyntax)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
