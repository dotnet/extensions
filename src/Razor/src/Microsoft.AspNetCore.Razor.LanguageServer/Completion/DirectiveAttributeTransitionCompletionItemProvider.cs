// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Razor.Completion;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class DirectiveAttributeTransitionCompletionItemProvider : DirectiveAttributeCompletionItemProviderBase
    {
        private static RazorCompletionItem _transitionCompletionItem;

        public static RazorCompletionItem TransitionCompletionItem
        {
            get
            {
                if (_transitionCompletionItem == null)
                {
                    _transitionCompletionItem = new RazorCompletionItem("@...", "@", RazorCompletionItemKind.Directive);
                    _transitionCompletionItem.SetDirectiveCompletionDescription(new DirectiveCompletionDescription("Blazor directive attributes"));
                }

                return _transitionCompletionItem;
            }
        }

        private static readonly IReadOnlyList<RazorCompletionItem> Completions = new[] { TransitionCompletionItem };

        public override IReadOnlyList<RazorCompletionItem> GetCompletionItems(RazorSyntaxTree syntaxTree, TagHelperDocumentContext tagHelperDocumentContext, SourceSpan location)
        {
            if (!FileKinds.IsComponent(syntaxTree.Options.FileKind))
            {
                // Directive attributes are only supported in components
                return Array.Empty<RazorCompletionItem>();
            }

            var change = new SourceChange(location, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                return Array.Empty<RazorCompletionItem>();
            }

            var attribute = owner.Parent;
            if (attribute is MarkupMiscAttributeContentSyntax)
            {
                // This represents a tag when there's no attribute content <InputText | />.
                return Completions;
            }

            if (!TryGetAttributeInfo(attribute, out var name, out var nameLocation, out _, out _))
            {
                return Array.Empty<RazorCompletionItem>();
            }

            if (name.StartsWith("@"))
            {
                // The transition is already provided
                return Array.Empty<RazorCompletionItem>();
            }

            if (!nameLocation.IntersectsWith(location.AbsoluteIndex))
            {
                // Not operating in the name section
                return Array.Empty<RazorCompletionItem>();
            }

            // This represents a tag when there's no attribute content <InputText | />.
            return Completions;
        }
    }
}
