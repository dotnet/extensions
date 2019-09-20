// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.Completion;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal static class CompletionItemExtensions
    {
        private const string TagHelperElementDataKey = "_TagHelperElementData_";
        private const string TagHelperAttributeDataKey = "_TagHelperAttributes_";
        private const string AttributeCompletionDataKey = "_AttributeCompletion_";
        private const string RazorCompletionItemKind = "_CompletionItemKind_";

        public static void SetRazorCompletionKind(this CompletionItem completion, RazorCompletionItemKind completionItemKind)
        {
            if (completion is null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            var data = completion.Data ?? new JObject();
            data[RazorCompletionItemKind] = JToken.FromObject(completionItemKind);
            completion.Data = data;
        }

        public static bool TryGetRazorCompletionKind(this CompletionItem completion, out RazorCompletionItemKind completionItemKind)
        {
            if (completion is null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            if (completion.Data is JObject data && data.ContainsKey(RazorCompletionItemKind))
            {
                completionItemKind = data[RazorCompletionItemKind].ToObject<RazorCompletionItemKind>();
                return true;
            }

            completionItemKind = default;
            return false;
        }

        public static bool IsTagHelperElementCompletion(this CompletionItem completion)
        {
            if (completion.Data is JObject data && data.ContainsKey(TagHelperElementDataKey))
            {
                return true;
            }

            return false;
        }

        public static bool IsTagHelperAttributeCompletion(this CompletionItem completion)
        {
            if (completion.Data is JObject data && data.ContainsKey(TagHelperAttributeDataKey))
            {
                return true;
            }

            return false;
        }

        public static void SetDescriptionInfo(this CompletionItem completion, ElementDescriptionInfo elementDescriptionInfo)
        {
            var data = completion.Data ?? new JObject();
            data[TagHelperElementDataKey] = JObject.FromObject(elementDescriptionInfo);
            completion.Data = data;
        }

        public static void SetDescriptionInfo(this CompletionItem completion, AttributeDescriptionInfo attributeDescriptionInfo)
        {
            var data = completion.Data ?? new JObject();
            data[TagHelperAttributeDataKey] = JObject.FromObject(attributeDescriptionInfo);
            completion.Data = data;
        }

        public static void SetDescriptionInfo(this CompletionItem completion, AttributeCompletionDescription attributeDescriptionInfo)
        {
            if (completion is null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            if (attributeDescriptionInfo is null)
            {
                throw new ArgumentNullException(nameof(attributeDescriptionInfo));
            }

            var data = completion.Data ?? new JObject();
            data[AttributeCompletionDataKey] = JObject.FromObject(attributeDescriptionInfo);
            completion.Data = data;
        }

        public static ElementDescriptionInfo GetElementDescriptionInfo(this CompletionItem completion)
        {
            if (completion.Data is JObject data && data.ContainsKey(TagHelperElementDataKey))
            {
                var descriptionInfo = data[TagHelperElementDataKey].ToObject<ElementDescriptionInfo>();
                return descriptionInfo;
            }

            return ElementDescriptionInfo.Default;
        }

        public static AttributeDescriptionInfo GetTagHelperAttributeDescriptionInfo(this CompletionItem completion)
        {
            if (completion.Data is JObject data && data.ContainsKey(TagHelperAttributeDataKey))
            {
                var descriptionInfo = data[TagHelperAttributeDataKey].ToObject<AttributeDescriptionInfo>();
                return descriptionInfo;
            }

            return AttributeDescriptionInfo.Default;
        }

        public static AttributeCompletionDescription GetAttributeDescriptionInfo(this CompletionItem completion)
        {
            if (completion is null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            if (completion.Data is JObject data && data.ContainsKey(AttributeCompletionDataKey))
            {
                var descriptionInfo = data[AttributeCompletionDataKey].ToObject<AttributeCompletionDescription>();
                return descriptionInfo;
            }

            return null;
        }
    }
}
