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

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal class DefaultTagHelperCompletionService : TagHelperCompletionService
    {
        private static readonly Container<string> AttributeCommitCharacters = new Container<string>(" ");
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

            if (TryGetAttributeInfo(parent, out containingTagNameToken, out var selectedAttributeName, out attributes) &&
                attributes.Span.IntersectsWith(location.AbsoluteIndex))
            {
                var stringifiedAttributes = StringifyAttributes(attributes);
                var attributeCompletions = GetAttributeCompletions(parent, containingTagNameToken.Content, selectedAttributeName, stringifiedAttributes, codeDocument);
                return attributeCompletions;
            }

            // Invalid location for TagHelper completions.
            return Array.Empty<CompletionItem>();
        }

        private static bool TryGetAttributeInfo(SyntaxNode attribute, out SyntaxToken containingTagNameToken, out string selectedAttributeName, out SyntaxList<RazorSyntaxNode> attributeNodes)
        {
            if ((attribute is MarkupMiscAttributeContentSyntax ||
                attribute is MarkupMinimizedAttributeBlockSyntax ||
                attribute is MarkupAttributeBlockSyntax ||
                attribute is MarkupTagHelperAttributeSyntax ||
                attribute is MarkupMinimizedTagHelperAttributeSyntax ||
                attribute is MarkupTagHelperDirectiveAttributeSyntax ||
                attribute is MarkupMinimizedTagHelperDirectiveAttributeSyntax) &&
                TryGetElementInfo(attribute.Parent, out containingTagNameToken, out attributeNodes))
            {
                selectedAttributeName = null;
                return true;
            }

            containingTagNameToken = null;
            selectedAttributeName = null;
            attributeNodes = default;
            return false;
        }

        private IReadOnlyList<CompletionItem> GetAttributeCompletions(
            SyntaxNode containingAttribute,
            string containingTagName,
            string selectedAttributeName,
            IEnumerable<KeyValuePair<string, string>> attributes,
            RazorCodeDocument codeDocument)
        {
            var ancestors = containingAttribute.Parent.Ancestors();
            var tagHelperDocumentContext = codeDocument.GetTagHelperContext();
            var nonDirectiveAttributeTagHelpers = tagHelperDocumentContext.TagHelpers.Where(tagHelper => !tagHelper.BoundAttributes.Any(attribute => attribute.IsDirectiveAttribute()));
            var filteredContext = TagHelperDocumentContext.Create(tagHelperDocumentContext.Prefix, nonDirectiveAttributeTagHelpers);
            var (ancestorTagName, ancestorIsTagHelper) = GetNearestAncestorTagInfo(ancestors);
            var attributeCompletionContext = new AttributeCompletionContext(
                filteredContext,
                existingCompletions: Enumerable.Empty<string>(),
                containingTagName,
                selectedAttributeName,
                attributes,
                ancestorTagName,
                ancestorIsTagHelper,
                HtmlSchemaTagNames.Contains);

            var completionItems = new List<CompletionItem>();
            var completionResult = _razorTagHelperCompletionService.GetAttributeCompletions(attributeCompletionContext);
            foreach (var completion in completionResult.Completions)
            {
                var filterText = completion.Key;

                // This is a little bit of a hack because the information returned by _razorTagHelperCompletionService.GetAttributeCompletions
                // does not have enough information for us to determine if a completion is an indexer completion or not. Therefore we have to
                // jump through a few hoops below to:
                //   1. Determine if this specific completion is an indexer based completion
                //   2. Resolve an appropriate snippet if it is. This is more troublesome because we need to remove the ... suffix to accurately
                //      build a snippet that makes sense for the user to type.
                var indexerCompletion = filterText.EndsWith("...");
                if (indexerCompletion)
                {
                    filterText = filterText.Substring(0, filterText.Length - 3);
                }

                var insertTextFormat = InsertTextFormat.Snippet;
                if (!TryResolveAttributeInsertionSnippet(filterText, completion.Value, indexerCompletion, out var insertText))
                {
                    insertTextFormat = InsertTextFormat.PlainText;
                    insertText = filterText;
                }

                var razorCompletionItem = new CompletionItem()
                {
                    Label = completion.Key,
                    InsertText = insertText,
                    InsertTextFormat = insertTextFormat,
                    FilterText = filterText,
                    SortText = filterText,
                    Kind = CompletionItemKind.TypeParameter,
                    CommitCharacters = AttributeCommitCharacters,
                };
                var attributeDescriptions = completion.Value.Select(boundAttribute => new TagHelperAttributeDescriptionInfo(
                    boundAttribute.DisplayName,
                    boundAttribute.GetPropertyName(),
                    indexerCompletion ? boundAttribute.IndexerTypeName : boundAttribute.TypeName,
                    boundAttribute.Documentation));
                var attributeDescriptionInfo = new AttributeDescriptionInfo(attributeDescriptions.ToList());
                razorCompletionItem.SetDescriptionInfo(attributeDescriptionInfo);

                completionItems.Add(razorCompletionItem);
            }

            return completionItems;
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
                var tagHelperDescriptions = completion.Value.Select(tagHelper => new TagHelperDescriptionInfo(tagHelper.GetTypeName(), tagHelper.Documentation));
                var elementDescription = new ElementDescriptionInfo(tagHelperDescriptions.ToList());
                razorCompletionItem.SetDescriptionInfo(elementDescription);

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
                else if (attribute is MarkupTagHelperDirectiveAttributeSyntax directiveAttribute)
                {
                    var name = directiveAttribute.FullName;
                    var value = directiveAttribute.Value?.GetContent() ?? string.Empty;
                    stringifiedAttributes.Add(new KeyValuePair<string, string>(name, value));
                }
                else if (attribute is MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute)
                {
                    var name = minimizedDirectiveAttribute.FullName;
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

        private bool TryResolveAttributeInsertionSnippet(
            string text,
            IEnumerable<BoundAttributeDescriptor> boundAttributes,
            bool indexerCompletion,
            out string snippetText)
        {
            const string BoolTypeName = "System.Boolean";

            // Boolean returning bound attribute, auto-complete to just the attribute name.
            if (indexerCompletion)
            {
                if (boundAttributes.All(boundAttribute => boundAttribute.IndexerTypeName == BoolTypeName))
                {
                    snippetText = null;
                    return false;
                }

                snippetText = string.Concat(text, "$1=\"$2\"");
                return true;
            }
            else if (boundAttributes.All(boundAttribute => boundAttribute.TypeName == BoolTypeName))
            {
                snippetText = null;
                return false;
            }

            snippetText = string.Concat(text, "=\"$1\"");
            return true;
        }
    }
}
