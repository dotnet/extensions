// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using RazorTagHelperCompletionService = Microsoft.VisualStudio.Editor.Razor.TagHelperCompletionService;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultTagHelperCompletionService : TagHelperCompletionService
    {
        private static readonly Container<string> ElementCommitCharacters = new Container<string>(" ", ">");
        private static readonly HashSet<string> HtmlSchemaTagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DOCTYPE",
            "a",
            "abbr",
            "acronym",
            "address",
            "applet",
            "area",
            "article",
            "aside",
            "audio",
            "b",
            "base",
            "basefont",
            "bdi",
            "bdo",
            "big",
            "blockquote",
            "body",
            "br",
            "button",
            "canvas",
            "caption",
            "center",
            "cite",
            "code",
            "col",
            "colgroup",
            "data",
            "datalist",
            "dd",
            "del",
            "details",
            "dfn",
            "dialog",
            "dir",
            "div",
            "dl",
            "dt",
            "em",
            "embed",
            "fieldset",
            "figcaption",
            "figure",
            "font",
            "footer",
            "form",
            "frame",
            "frameset",
            "h1",
            "h2",
            "h3",
            "h4",
            "h5",
            "h6",
            "head",
            "header",
            "hr",
            "html",
            "i",
            "iframe",
            "img",
            "input",
            "ins",
            "kbd",
            "label",
            "legend",
            "li",
            "link",
            "main",
            "map",
            "mark",
            "meta",
            "meter",
            "nav",
            "noframes",
            "noscript",
            "object",
            "ol",
            "optgroup",
            "option",
            "output",
            "p",
            "param",
            "picture",
            "pre",
            "progress",
            "q",
            "rp",
            "rt",
            "ruby",
            "s",
            "samp",
            "script",
            "section",
            "select",
            "small",
            "source",
            "span",
            "strike",
            "strong",
            "style",
            "sub",
            "summary",
            "sup",
            "svg",
            "table",
            "tbody",
            "td",
            "template",
            "textarea",
            "tfoot",
            "th",
            "thead",
            "time",
            "title",
            "tr",
            "track",
            "tt",
            "u",
            "ul",
            "var",
            "video",
            "wbr",
        };
        private readonly RazorTagHelperCompletionService _razorTagHelperCompletionService;

        public DefaultTagHelperCompletionService(RazorTagHelperCompletionService razorCompletionService)
        {
            if (razorCompletionService == null)
            {
                throw new ArgumentNullException(nameof(razorCompletionService));
            }

            _razorTagHelperCompletionService = razorCompletionService;
        }

        public override IReadOnlyList<CompletionItem> GetCompletionsAt(SourceSpan location, RazorCodeDocument codeDocument)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            var syntaxTree = codeDocument.GetSyntaxTree();
            var change = new SourceChange(location, "");
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                Debug.Fail("Owner should never be null.");
                return Array.Empty<CompletionItem>();
            }

            var parent = owner.Parent;
            if (TryGetElementInfo(parent, out var containingTagNameToken, out var attributes) &&
                containingTagNameToken.Span.IntersectsWith(location.AbsoluteIndex))
            {
                var stringifiedAttributes = StringifyAttributes(attributes);
                var elementCompletions = GetElementCompletions(parent, containingTagNameToken.Content, stringifiedAttributes, codeDocument);
                return elementCompletions;
            }

            // Invalid location for TagHelper completions.
            return Array.Empty<CompletionItem>();
        }

        private IReadOnlyList<CompletionItem> GetElementCompletions(
            SyntaxNode containingTag,
            string containingTagName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            RazorCodeDocument codeDocument)
        {
            var ancestors = containingTag.Ancestors();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();
            var (ancestorTagName, ancestorIsTagHelper) = GetNearestAncestorTagInfo(ancestors);
            var elementCompletionContext = new ElementCompletionContext(
                tagHelperDocumentContext,
                existingCompletions: Enumerable.Empty<string>(),
                containingTagName,
                attributes,
                ancestorTagName,
                ancestorIsTagHelper,
                HtmlSchemaTagNames.Contains);

            var completionItems = new List<CompletionItem>();
            var completionResult = _razorTagHelperCompletionService.GetElementCompletions(elementCompletionContext);
            foreach (var completion in completionResult.Completions)
            {
                var razorCompletionItem = new CompletionItem()
                {
                    Label = completion.Key,
                    InsertText = completion.Key,
                    FilterText = completion.Key,
                    SortText = completion.Key,
                    Kind = CompletionItemKind.TypeParameter,
                    CommitCharacters = ElementCommitCharacters,
                };

                completionItems.Add(razorCompletionItem);
            }

            return completionItems;
        }

        // Internal for testing
        internal static IEnumerable<KeyValuePair<string, string>> StringifyAttributes(SyntaxList<RazorSyntaxNode> attributes)
        {
            var stringifiedAttributes = new List<KeyValuePair<string, string>>();

            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                if (attribute is MarkupTagHelperAttributeSyntax tagHelperAttribute)
                {
                    var name = tagHelperAttribute.Name.GetContent();
                    var value = tagHelperAttribute.Value?.GetContent() ?? string.Empty;
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, value));
                }
                else if (attribute is MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute)
                {
                    var name = minimizedTagHelperAttribute.Name.GetContent();
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, string.Empty));
                }
                else if (attribute is MarkupAttributeBlockSyntax markupAttribute)
                {
                    var name = markupAttribute.Name.GetContent();
                    var value = markupAttribute.Value?.GetContent() ?? string.Empty;
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, value));
                }
                else if (attribute is MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute)
                {
                    var name = minimizedMarkupAttribute.Name.GetContent();
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, string.Empty));
                }
            }

            return stringifiedAttributes;
        }

        // Internal for testing
        internal static (string ancestorTagName, bool ancestorIsTagHelper) GetNearestAncestorTagInfo(IEnumerable<SyntaxNode> ancestors)
        {
            foreach (var ancestor in ancestors)
            {
                if (ancestor is MarkupElementSyntax element)
                {
                    // It's possible for start tag to be null in malformed cases.
                    var name = element.StartTag?.Name?.Content ?? string.Empty;
                    return (name, ancestorIsTagHelper: false);
                }
                else if (ancestor is MarkupTagHelperElementSyntax tagHelperElement)
                {
                    // It's possible for start tag to be null in malformed cases.
                    var name = tagHelperElement.StartTag?.Name?.Content ?? string.Empty;
                    return (name, ancestorIsTagHelper: true);
                }
            }

            return (ancestorTagName: null, ancestorIsTagHelper: false);
        }

        private static bool TryGetElementInfo(SyntaxNode element, out SyntaxToken containingTagNameToken, out SyntaxList<RazorSyntaxNode> attributeNodes)
        {
            if (element is MarkupStartTagSyntax startTag)
            {
                containingTagNameToken = startTag.Name;
                attributeNodes = startTag.Attributes;
                return true;
            }

            if (element is MarkupTagHelperStartTagSyntax startTagHelper)
            {
                containingTagNameToken = startTagHelper.Name;
                attributeNodes = startTagHelper.Attributes;
                return true;
            }

            containingTagNameToken = null;
            attributeNodes = default;
            return false;
        }
    }
}
