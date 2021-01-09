// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Razor.Completion;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal static class RazorCompletionItemExtensions
    {
        private readonly static string TagHelperAttributeCompletionDescriptionKey = "Razor.TagHelperAttributeDescription";
        private readonly static string TagHelperElementCompletionDescriptionKey = "Razor.TagHelperElementDescription";

        public static void SetTagHelperElementDescriptionInfo(this RazorCompletionItem completionItem, ElementDescriptionInfo elementDescriptionInfo)
        {
            completionItem.Items[TagHelperElementCompletionDescriptionKey] = elementDescriptionInfo;
        }

        public static ElementDescriptionInfo? GetTagHelperElementDescriptionInfo(this RazorCompletionItem completionItem)
        {
            if (completionItem is null)
            {
                throw new ArgumentNullException(nameof(completionItem));
            }

            var description = completionItem.Items[TagHelperElementCompletionDescriptionKey] as ElementDescriptionInfo;
            return description;
        }

        public static void SetTagHelperAttributeDescriptionInfo(this RazorCompletionItem completionItem, AttributeDescriptionInfo attributeDescriptionInfo)
        {
            if (completionItem is null)
            {
                throw new ArgumentNullException(nameof(completionItem));
            }

            completionItem.Items[TagHelperAttributeCompletionDescriptionKey] = attributeDescriptionInfo;
        }

        public static AttributeDescriptionInfo? GetTagHelperAttributeDescriptionInfo(this RazorCompletionItem completionItem)
        {
            if (completionItem is null)
            {
                throw new ArgumentNullException(nameof(completionItem));
            }

            var description = completionItem.Items[TagHelperAttributeCompletionDescriptionKey] as AttributeDescriptionInfo;
            return description;
        }
    }
}
