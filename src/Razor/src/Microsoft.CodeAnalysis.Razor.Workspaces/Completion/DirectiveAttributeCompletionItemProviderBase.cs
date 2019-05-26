// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using RazorSyntaxList = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxList<Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode>;
using RazorSyntaxNode = Microsoft.AspNetCore.Razor.Language.Syntax.SyntaxNode;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    internal abstract class DirectiveAttributeCompletionItemProviderBase : RazorCompletionItemProvider
    {
        // Internal for testing
        internal static bool TryGetAttributeInfo(
            RazorSyntaxNode attributeLeafOwner,
            out string name,
            out TextSpan nameLocation,
            out string parameterName,
            out TextSpan parameterLocation)
        {
            var attribute = attributeLeafOwner.Parent;

            switch (attribute)
            {
                case MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute:
                    TryExtractIncompleteDirectiveAttribute(
                        minimizedMarkupAttribute.Name.GetContent(),
                        minimizedMarkupAttribute.Name.Span,
                        out name,
                        out nameLocation,
                        out parameterName,
                        out parameterLocation);

                    return true;
                case MarkupAttributeBlockSyntax markupAttribute:
                    TryExtractIncompleteDirectiveAttribute(
                        markupAttribute.Name.GetContent(),
                        markupAttribute.Name.Span,
                        out name,
                        out nameLocation,
                        out parameterName,
                        out parameterLocation);
                    return true;
                case MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute:
                    TryExtractIncompleteDirectiveAttribute(
                        minimizedTagHelperAttribute.Name.GetContent(),
                        minimizedTagHelperAttribute.Name.Span,
                        out name,
                        out nameLocation,
                        out parameterName,
                        out parameterLocation);
                    return true;
                case MarkupTagHelperAttributeSyntax tagHelperAttribute:
                    TryExtractIncompleteDirectiveAttribute(
                        tagHelperAttribute.Name.GetContent(),
                        tagHelperAttribute.Name.Span,
                        out name,
                        out nameLocation,
                        out parameterName,
                        out parameterLocation);
                    return true;
                case MarkupTagHelperDirectiveAttributeSyntax directiveAttribute:
                    {
                        var attributeName = directiveAttribute.Name;
                        var directiveAttributeTransition = directiveAttribute.Transition;
                        var nameStart = directiveAttributeTransition?.SpanStart ?? attributeName.SpanStart;
                        var nameEnd = attributeName?.Span.End ?? directiveAttributeTransition.Span.End;
                        name = string.Concat(directiveAttributeTransition?.GetContent(), attributeName?.GetContent());
                        nameLocation = new TextSpan(nameStart, nameEnd - nameStart);
                        parameterName = directiveAttribute.ParameterName?.GetContent();
                        parameterLocation = directiveAttribute.ParameterName?.Span ?? default;
                        return true;
                    }
                case MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute:
                    {
                        var attributeName = minimizedDirectiveAttribute.Name;
                        var directiveAttributeTransition = minimizedDirectiveAttribute.Transition;
                        var nameStart = directiveAttributeTransition?.SpanStart ?? attributeName.SpanStart;
                        var nameEnd = attributeName?.Span.End ?? directiveAttributeTransition.Span.End;
                        name = string.Concat(directiveAttributeTransition?.GetContent(), attributeName?.GetContent());
                        nameLocation = new TextSpan(nameStart, nameEnd - nameStart);
                        parameterName = minimizedDirectiveAttribute.ParameterName?.GetContent();
                        parameterLocation = minimizedDirectiveAttribute.ParameterName?.Span ?? default;
                        return true;
                    }
            }

            name = null;
            nameLocation = default;
            parameterName = null;
            parameterLocation = default;
            return false;
        }

        // Internal for testing
        internal static bool TryGetElementInfo(RazorSyntaxNode element, out string containingTagName, out IEnumerable<string> attributeNames)
        {
            if (element is MarkupStartTagSyntax startTag)
            {
                containingTagName = startTag.Name.GetContent();
                attributeNames = ExtractAttributeNames(startTag.Attributes);
                return true;
            }

            if (element is MarkupTagHelperStartTagSyntax startTagHelper)
            {
                containingTagName = startTagHelper.Name.GetContent();
                attributeNames = ExtractAttributeNames(startTagHelper.Attributes);
                return true;
            }

            containingTagName = null;
            attributeNames = default;
            return false;
        }

        private static IEnumerable<string> ExtractAttributeNames(RazorSyntaxList attributes)
        {
            var attributeNames = new List<string>();

            for (var i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];
                if (attribute is MarkupTagHelperAttributeSyntax tagHelperAttribute)
                {
                    var name = tagHelperAttribute.Name.GetContent();
                    attributeNames.Add(name);
                }
                else if (attribute is MarkupMinimizedTagHelperAttributeSyntax minimizedTagHelperAttribute)
                {
                    var name = minimizedTagHelperAttribute.Name.GetContent();
                    attributeNames.Add(name);
                }
                else if (attribute is MarkupAttributeBlockSyntax markupAttribute)
                {
                    var name = markupAttribute.Name.GetContent();
                    attributeNames.Add(name);
                }
                else if (attribute is MarkupMinimizedAttributeBlockSyntax minimizedMarkupAttribute)
                {
                    var name = minimizedMarkupAttribute.Name.GetContent();
                    attributeNames.Add(name);
                }
                else if (attribute is MarkupTagHelperDirectiveAttributeSyntax directiveAttribute)
                {
                    var name = directiveAttribute.FullName;
                    attributeNames.Add(name);
                }
                else if (attribute is MarkupMinimizedTagHelperDirectiveAttributeSyntax minimizedDirectiveAttribute)
                {
                    var name = minimizedDirectiveAttribute.FullName;
                    attributeNames.Add(name);
                }
            }

            return attributeNames;
        }

        private static void TryExtractIncompleteDirectiveAttribute(
            string attributeName,
            TextSpan attributeNameLocation,
            out string name,
            out TextSpan nameLocation,
            out string parameterName,
            out TextSpan parameterLocation)
        {
            name = attributeName;
            nameLocation = attributeNameLocation;
            parameterName = default;
            parameterLocation = default;

            // It's possible that the attribute looks like a directive attribute but is incomplete. 
            // We should try and extract out the transition and parameter.

            if (!attributeName.StartsWith("@"))
            {
                // Doesn't look like a directive attribute. Not an incomplete directive attribute.
                return;
            }

            var colonIndex = attributeName.IndexOf(':');
            if (colonIndex == -1)
            {
                // There's no parameter, the existing attribute name and location is sufficient.
                return;
            }

            parameterName = attributeName.Substring(colonIndex + 1);
            parameterLocation = new TextSpan(attributeNameLocation.Start + colonIndex + 1, parameterName.Length);
            name = attributeName.Substring(0, colonIndex);
            nameLocation = new TextSpan(attributeNameLocation.Start, name.Length);
        }
    }
}
