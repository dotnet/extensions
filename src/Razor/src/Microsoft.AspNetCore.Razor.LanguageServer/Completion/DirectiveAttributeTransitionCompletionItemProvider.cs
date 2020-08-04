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

            if (!TryGetAttributeInfo(owner, out var prefixLocation, out var attributeName, out var attributeNameLocation, out _, out _))
            {
                return Array.Empty<RazorCompletionItem>();
            }

            if (attributeNameLocation.IntersectsWith(location.AbsoluteIndex) && attributeName.StartsWith("@", StringComparison.Ordinal))
            {
                // The transition is already provided for the attribute name
                return Array.Empty<RazorCompletionItem>();
            }

            if (!IsValidCompletionPoint(location, prefixLocation, attributeNameLocation))
            {
                // Not operating in the attribute name area
                return Array.Empty<RazorCompletionItem>();
            }

            // This represents a tag when there's no attribute content <InputText | />.
            return Completions;
        }

        // Internal for testing
        internal static bool IsValidCompletionPoint(SourceSpan location, TextSpan? prefixLocation, TextSpan attributeNameLocation)
        {
            if (location.AbsoluteIndex == (prefixLocation?.Start ?? -1))
            {
                // <input| class="test" />
                // Starts of prefix locations belong to the previous SyntaxNode. It could be the end of an attribute value, the tag name, C# etc.
                return false;
            }

            if (attributeNameLocation.Start == location.AbsoluteIndex)
            {
                // <input |class="test" />
                return false;
            }

            if (prefixLocation?.IntersectsWith(location.AbsoluteIndex) ?? false)
            {
                // <input   |  class="test" />
                return true;
            }

            if (attributeNameLocation.IntersectsWith(location.AbsoluteIndex))
            {
                // <input cla|ss="test" />
                return false;
            }

            return false;
        }
    }
}
