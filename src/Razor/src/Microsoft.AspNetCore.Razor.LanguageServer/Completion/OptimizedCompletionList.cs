// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    [JsonConverter(typeof(OptimizedCompletionListJsonConverter))]
    internal class OptimizedCompletionList : CompletionList
    {
        public OptimizedCompletionList(CompletionList innerList) : base(innerList.Items, innerList.IsIncomplete)
        {
        }

        public class OptimizedCompletionListJsonConverter : JsonConverter
        {
            public static readonly OptimizedCompletionListJsonConverter Instance = new OptimizedCompletionListJsonConverter();
            private static readonly ConcurrentDictionary<object, string> CommitCharactersRawJson;
            private static readonly JsonSerializer DefaultSerializer;

            static OptimizedCompletionListJsonConverter()
            {
                DefaultSerializer = JsonSerializer.CreateDefault();
                CommitCharactersRawJson = new ConcurrentDictionary<object, string>();
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(OptimizedCompletionList).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var result = DefaultSerializer.Deserialize(reader, objectType);
                return result;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var completionList = (CompletionList)value;

                writer.WriteStartObject();

                WriteCompletionListProperties(writer, completionList, serializer);

                if (completionList.Items != null)
                {
                    writer.WritePropertyName("items");

                    writer.WriteStartArray();
                    foreach (var completionItem in completionList.Items)
                    {
                        if (completionItem == null)
                        {
                            continue;
                        }

                        // HACK: O# will automatically inject data objects on serialization start; however, VS already captures
                        // this for us if the Data field on the completion list is unempty. In future O# versions (0.19+)we can
                        // remove this hack because O# will no longer inject serialization items when not required.
                        var suppressData = (completionList as VSCompletionList)?.Data != null;
                        WriteCompletionItem(writer, completionItem, serializer, suppressData);
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            protected virtual void WriteCompletionListProperties(JsonWriter writer, CompletionList completionList, JsonSerializer serializer)
            {
                writer.WritePropertyName("isIncomplete");
                writer.WriteValue(completionList.IsIncomplete);
            }

            private void WriteCompletionItem(JsonWriter writer, CompletionItem completionItem, JsonSerializer serializer, bool suppressData)
            {
                writer.WriteStartObject();

                var label = completionItem.Label;
                if (label != null)
                {
                    writer.WritePropertyName("label");
                    writer.WriteValue(label);
                }

                writer.WritePropertyName("kind");
                writer.WriteValue(completionItem.Kind);

                if (completionItem.Detail != null)
                {
                    writer.WritePropertyName("detail");
                    writer.WriteValue(completionItem.Detail);
                }

                if (completionItem.Documentation != null)
                {
                    writer.WritePropertyName("documentation");
                    serializer.Serialize(writer, completionItem.Documentation);
                }

                // Only render preselect if it's "true"
                if (completionItem.Preselect)
                {
                    writer.WritePropertyName("preselect");
                    writer.WriteValue(completionItem.Preselect);
                }

                if (completionItem.SortText != null && !string.Equals(completionItem.SortText, label, StringComparison.Ordinal))
                {
                    writer.WritePropertyName("sortText");
                    writer.WriteValue(completionItem.SortText);
                }

                if (completionItem.FilterText != null && !string.Equals(completionItem.FilterText, label, StringComparison.Ordinal))
                {
                    writer.WritePropertyName("filterText");
                    writer.WriteValue(completionItem.FilterText);
                }

                if (completionItem.InsertText != null && !string.Equals(completionItem.InsertText, label, StringComparison.Ordinal))
                {
                    writer.WritePropertyName("insertText");
                    writer.WriteValue(completionItem.InsertText);
                }

                if (completionItem.InsertTextFormat != default && completionItem.InsertTextFormat != InsertTextFormat.PlainText)
                {
                    writer.WritePropertyName("insertTextFormat");
                    writer.WriteValue(completionItem.InsertTextFormat);
                }

                if (completionItem.TextEdit != null)
                {
                    writer.WritePropertyName("textEdit");
                    serializer.Serialize(writer, completionItem.TextEdit);
                }

                if (completionItem.AdditionalTextEdits != null && completionItem.AdditionalTextEdits.Any())
                {
                    writer.WritePropertyName("additionalTextEdits");
                    serializer.Serialize(writer, completionItem.AdditionalTextEdits);
                }

                if (completionItem.CommitCharacters != null && completionItem.CommitCharacters.Any())
                {
                    writer.WritePropertyName("commitCharacters");

                    if (!CommitCharactersRawJson.TryGetValue(completionItem.CommitCharacters, out var jsonString))
                    {
                        jsonString = JsonConvert.SerializeObject(completionItem.CommitCharacters);
                        CommitCharactersRawJson.TryAdd(completionItem.CommitCharacters, jsonString);
                    }

                    writer.WriteRawValue(jsonString);
                }

                if (completionItem.Command != null)
                {
                    writer.WritePropertyName("command");
                    serializer.Serialize(writer, completionItem.Command);
                }

                if (!suppressData && completionItem.Data != null)
                {
                    writer.WritePropertyName("data");
                    serializer.Serialize(writer, completionItem.Data);
                }

                writer.WriteEndObject();
            }
        }
    }
}
