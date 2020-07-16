// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Semantic
{
    internal class SemanticTokensOrSemanticTokensEditsConverter : JsonConverter<SemanticTokensOrSemanticTokensEdits>
    {
        public static readonly SemanticTokensOrSemanticTokensEditsConverter Instance = new SemanticTokensOrSemanticTokensEditsConverter();

        public override void WriteJson(JsonWriter writer, SemanticTokensOrSemanticTokensEdits edits, JsonSerializer serializer)
        {
            if (edits.IsSemanticTokens)
            {
                serializer.Serialize(writer, edits.SemanticTokens);
            }
            else if (edits.IsSemanticTokensEdits)
            {
                serializer.Serialize(writer, edits.SemanticTokensEdits);
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override SemanticTokensOrSemanticTokensEdits ReadJson(JsonReader reader, Type objectType, SemanticTokensOrSemanticTokensEdits existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            if (obj["data"] is null)
            {
                return new SemanticTokensOrSemanticTokensEdits(obj.ToObject<SemanticTokensEditCollection>());
            }
            else
            {
                return new SemanticTokensOrSemanticTokensEdits(obj.ToObject<SemanticTokens>());
            }
        }

        public override bool CanRead => true;
    }
}
