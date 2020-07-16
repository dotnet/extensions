// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    internal class AttributeSnippetOnAutoInsertProvider : RazorOnAutoInsertProvider
    {
        private readonly TagHelperFactsService _tagHelperFactsService;

        public override string TriggerCharacter => "=";

        public AttributeSnippetOnAutoInsertProvider(TagHelperFactsService tagHelperFactsService)
        {
            if (tagHelperFactsService is null)
            {
                throw new ArgumentNullException(nameof(tagHelperFactsService));
            }

            _tagHelperFactsService = tagHelperFactsService;
        }

        public override bool TryResolveInsertion(Position position, FormattingContext context, out TextEdit edit, out InsertTextFormat format)
        {
            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (!IsAtAttributeValueStart(context, position))
            {
                format = default;
                edit = default;
                return false;
            }

            // We've just typed a Razor comment start.
            format = InsertTextFormat.Snippet;
            edit = new TextEdit()
            {
                NewText = "\"$0\"",
                Range = new Range(position, position)
            };

            return true;
        }

        private bool IsAtAttributeValueStart(FormattingContext context, Position position)
        {
            var syntaxTree = context.CodeDocument.GetSyntaxTree();

            var absoluteIndex = position.GetAbsoluteIndex(context.SourceText);
            var change = new SourceChange(absoluteIndex, 0, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner?.Parent is MarkupLiteralAttributeValueSyntax)
            {
                // Accounts for
                // 1. <Counter IncrementBy=|
                // 2. <Counter IncrementBy|
                // 3. <Counter IncrementBy=|
                owner = owner.Parent;
            }

            if (owner is null ||
                !((owner.Parent is MarkupTagHelperAttributeValueSyntax attributeValue) &&
                (owner.Parent.Parent is MarkupTagHelperAttributeSyntax attribute) &&
                (owner.Parent.Parent.Parent is MarkupTagHelperStartTagSyntax startTag) &&
                (owner.Parent.Parent.Parent.Parent is MarkupTagHelperElementSyntax tagHelperElement)))
            {
                // Incorrect taghelper tree structure
                return false;
            }

            if (!attributeValue.Span.IsEmpty || string.IsNullOrEmpty(attribute.Name.GetContent()))
            {
                // Attribute value already started or attribute is empty
                return false;
            }

            var boundAttributes = _tagHelperFactsService.GetBoundTagHelperAttributes(context.CodeDocument.GetTagHelperContext(), attribute.Name.GetContent(), tagHelperElement.TagHelperInfo.BindingResult);
            var isStringProperty = boundAttributes.FirstOrDefault(a => a.Name == attribute.Name.GetContent())?.IsStringProperty ?? true;

            return !isStringProperty;
        }
    }
}
