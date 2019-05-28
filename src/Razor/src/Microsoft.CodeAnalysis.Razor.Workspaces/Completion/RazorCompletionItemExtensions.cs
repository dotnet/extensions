// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    internal static class RazorCompletionItemExtensions
    {
        private readonly static string AttributeCompletionDescriptionKey = "Razor.AttributeDescription";
        private readonly static string DirectiveCompletionDescriptionKey = "Razor.DirectiveDescription";

        public static void SetAttributeCompletionDescription(this RazorCompletionItem completionItem, AttributeCompletionDescription attributeCompletionDescription)
        {
            if (completionItem is null)
            {
                throw new ArgumentNullException(nameof(completionItem));
            }

            completionItem.Items[AttributeCompletionDescriptionKey] = attributeCompletionDescription;
        }

        public static AttributeCompletionDescription GetAttributeCompletionDescription(this RazorCompletionItem completionItem)
        {
            if (completionItem is null)
            {
                throw new ArgumentNullException(nameof(completionItem));
            }

            var attributeCompletionDescription = completionItem.Items[AttributeCompletionDescriptionKey] as AttributeCompletionDescription;
            return attributeCompletionDescription;
        }

        public static void SetDirectiveCompletionDescription(this RazorCompletionItem completionItem, DirectiveCompletionDescription attributeCompletionDescription)
        {
            if (completionItem is null)
            {
                throw new ArgumentNullException(nameof(completionItem));
            }

            completionItem.Items[DirectiveCompletionDescriptionKey] = attributeCompletionDescription;
        }

        public static DirectiveCompletionDescription GetDirectiveCompletionDescription(this RazorCompletionItem completionItem)
        {
            if (completionItem is null)
            {
                throw new ArgumentNullException(nameof(completionItem));
            }

            var attributeCompletionDescription = completionItem.Items[DirectiveCompletionDescriptionKey] as DirectiveCompletionDescription;
            return attributeCompletionDescription;
        }
    }
}