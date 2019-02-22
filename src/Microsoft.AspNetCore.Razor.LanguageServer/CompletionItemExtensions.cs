// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class CompletionItemExtensions
    {
        private const string TagHelperElementDataKey = "_TagHelperElementData_";

        public static bool IsTagHelperElementCompletion(this CompletionItem completion)
        {
            if (completion.Data is JObject data && data.ContainsKey(TagHelperElementDataKey))
            {
                return true;
            }

            return false;
        }

        public static void SetDescriptionData(this CompletionItem completion, ElementDescriptionInfo elementDescriptionInfo)
        {
            var data = new JObject();
            data[TagHelperElementDataKey] = JObject.FromObject(elementDescriptionInfo);
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
    }
}
