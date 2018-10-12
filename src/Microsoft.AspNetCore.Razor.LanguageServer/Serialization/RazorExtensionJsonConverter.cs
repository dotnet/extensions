// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This class is a copy from the Razor repo.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Serialization
{
    internal class RazorExtensionJsonConverter : JsonConverter
    {
        public static readonly RazorExtensionJsonConverter Instance = new RazorExtensionJsonConverter();

        public override bool CanConvert(Type objectType)
        {
            return typeof(RazorExtension).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            var obj = JObject.Load(reader);
            var extensionName = obj[nameof(RazorExtension.ExtensionName)].Value<string>();

            return new SerializedRazorExtension(extensionName);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var extension = (RazorExtension)value;

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(RazorExtension.ExtensionName));
            writer.WriteValue(extension.ExtensionName);

            writer.WriteEndObject();
        }
    }
}
