// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal static class RazorCompletionItemExtensions
    {
        private readonly static string TagHelperElementCompletionDescriptionKey = "Razor.TagHelperElementDescription";

        public static void SetTagHelperElementDescriptionInfo(this RazorCompletionItem completionItem, AggregateBoundElementDescription elementDescriptionInfo)
        {
            completionItem.Items[TagHelperElementCompletionDescriptionKey] = elementDescriptionInfo;
        }

        public static AggregateBoundElementDescription? GetTagHelperElementDescriptionInfo(this RazorCompletionItem completionItem)
        {
            if (completionItem is null)
            {
                throw new ArgumentNullException(nameof(completionItem));
            }

            var description = completionItem.Items[TagHelperElementCompletionDescriptionKey] as AggregateBoundElementDescription;
            return description;
        }
    }
}
