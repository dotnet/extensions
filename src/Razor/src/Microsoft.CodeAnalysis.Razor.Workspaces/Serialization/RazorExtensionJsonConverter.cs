// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor.Serialization
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

            var extensionName = string.Empty;

            reader.ReadProperties(propertyName =>
            {
                switch (propertyName)
                {
                    case nameof(RazorExtension.ExtensionName):
                        if (reader.Read())
                        {
                            extensionName = (string)reader.Value;
                        }
                        break;
                }
            });

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
