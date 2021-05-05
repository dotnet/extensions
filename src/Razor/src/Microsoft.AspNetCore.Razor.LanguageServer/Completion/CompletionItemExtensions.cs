// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    internal static class CompletionItemExtensions
    {
        private const string ResultIdKey = "_resultId";

        public static void SetCompletionListResultId(this CompletionItem completion, long resultId)
        {
            if (completion is null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            var data = completion.Data ?? new JObject();
            data[ResultIdKey] = resultId;
            completion.Data = data;
        }

        public static bool TryGetCompletionListResultId(this CompletionItem completion, out int resultId)
        {
            if (completion is null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            if (completion.Data is JObject data && data.ContainsKey(ResultIdKey))
            {
                resultId = data[ResultIdKey].ToObject<int>();
                return true;
            }

            resultId = default;
            return false;
        }

        public static VSCompletionItem ToVSCompletionItem(this CompletionItem completion)
        {
            if (completion is null)
            {
                throw new ArgumentNullException(nameof(completion));
            }

            return new VSCompletionItem
            {
                AdditionalTextEdits = completion.AdditionalTextEdits,
                Command = completion.Command,
                CommitCharacters = completion.CommitCharacters,
                Data = completion.Data,
                Deprecated = completion.Deprecated,
                Detail = completion.Detail,
                Documentation = completion.Documentation,
                FilterText = completion.FilterText,
                InsertText = completion.FilterText,
                InsertTextFormat = completion.InsertTextFormat,
                Kind = completion.Kind,
                Label = completion.Label,
                Preselect = completion.Preselect,
                SortText = completion.SortText,
                Tags = completion.Tags,
                TextEdit = completion.TextEdit,
            };
        }
    }
}
