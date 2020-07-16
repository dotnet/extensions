// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor.Serialization
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

            var configurationName = reader.ReadNextStringProperty(nameof(RazorConfiguration.ConfigurationName));
            var languageVersionValue = reader.ReadNextStringProperty(nameof(RazorConfiguration.LanguageVersion));
            var extensions = reader.ReadPropertyArray<RazorExtension>(serializer, nameof(RazorConfiguration.Extensions));

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
