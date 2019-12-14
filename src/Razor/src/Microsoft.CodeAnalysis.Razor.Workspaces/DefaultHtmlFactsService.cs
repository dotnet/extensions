// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.VisualStudio.Editor.Razor
{
    internal class DefaultHtmlFactsService : HtmlFactsService
    {
        public override bool TryGetElementInfo(SyntaxNode element, out SyntaxToken containingTagNameToken, out SyntaxList<RazorSyntaxNode> attributeNodes)
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

        public override bool TryGetAttributeInfo(SyntaxNode attribute, out SyntaxToken containingTagNameToken, out string selectedAttributeName, out SyntaxList<RazorSyntaxNode> attributeNodes)
        {
            if (!TryGetElementInfo(attribute.Parent, out containingTagNameToken, out attributeNodes))
            {
                containingTagNameToken = null;
                selectedAttributeName = null;
                attributeNodes = default;
                return false;
            }

            if (attribute is MarkupMinimizedAttributeBlockSyntax minimizedAttributeBlock)
            {
                selectedAttributeName = minimizedAttributeBlock.Name.GetContent();
                return true;
            }
            else if (attribute is MarkupAttributeBlockSyntax attributeBlock)
            {
                selectedAttributeName = attributeBlock.Name.GetContent();
                return true;
            }
            else if (attribute is MarkupTagHelperAttributeSyntax tagHelperAttribute)
            {
                selectedAttributeName = tagHelperAttribute.Name.GetContent();
                return true;
            }
            else if (attribute is MarkupMinimizedTagHelperAttributeSyntax minimizedAttribute)
            {
                selectedAttributeName = minimizedAttribute.Name.GetContent();
                return true;
            }
            else if (attribute is MarkupTagHelperDirectiveAttributeSyntax tagHelperDirectiveAttribute)
            {
                selectedAttributeName = tagHelperDirectiveAttribute.Name.GetContent();
                return true;
            }
            else if (attribute is MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedTagHelperDirectiveAttribute)
            {
                selectedAttributeName = minimizedTagHelperDirectiveAttribute.Name.GetContent();
                return true;
            }
            else if (attribute is MarkupMiscAttributeContentSyntax)
            {
                selectedAttributeName = null;
                return true;
            }

            // Not an attribute type that we know of
            selectedAttributeName = null;
            return false;
        }
    }
}
