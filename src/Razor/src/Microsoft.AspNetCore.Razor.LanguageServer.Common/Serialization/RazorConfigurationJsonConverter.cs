// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This class is a copy from the Razor repo.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization
{
    internal class RazorConfigurationJsonConverter : JsonConverter
    {
        public static readonly RazorConfigurationJsonConverter Instance = new RazorConfigurationJsonConverter();

        public override bool CanConvert(Type objectType)
        {
            return typeof(RazorConfiguration).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            var obj = JObject.Load(reader);
            var configurationName = obj[nameof(RazorConfiguration.ConfigurationName)].Value<string>();
            var languageVersionValue = obj[nameof(RazorConfiguration.LanguageVersion)].Value<string>();
            var extensions = obj[nameof(RazorConfiguration.Extensions)].ToObject<RazorExtension[]>(serializer);

            if (!RazorLanguageVersion.TryParse(languageVersionValue, out var languageVersion))
            {
                languageVersion = RazorLanguageVersion.Version_2_1;
            }

            return RazorConfiguration.Create(languageVersion, configurationName, extensions);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var configuration = (RazorConfiguration)value;

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(RazorConfiguration.ConfigurationName));
            writer.WriteValue(configuration.ConfigurationName);

            writer.WritePropertyName(nameof(RazorConfiguration.LanguageVersion));
            if (configuration.LanguageVersion == RazorLanguageVersion.Experimental)
            {
                writer.WriteValue("Experimental");
            }
            else
            {
                writer.WriteValue(configuration.LanguageVersion.ToString());
            }

            writer.WritePropertyName(nameof(RazorConfiguration.Extensions));
            serializer.Serialize(writer, configuration.Extensions);

            writer.WriteEndObject();
        }
    }
}
