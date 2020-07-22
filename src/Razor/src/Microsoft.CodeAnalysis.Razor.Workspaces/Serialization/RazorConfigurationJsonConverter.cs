// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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

            string configurationName = null;
            string languageVersionValue = null;
            IReadOnlyList<RazorExtension> extensions = null;

            reader.ReadProperties(propertyName =>
            {
                switch (propertyName)
                {
                    case nameof(RazorConfiguration.ConfigurationName):
                        if (reader.Read())
                        {
                            configurationName = (string)reader.Value;
                        }
                        break;
                    case nameof(RazorConfiguration.LanguageVersion):
                        if (reader.Read())
                        {
                            languageVersionValue = reader.Value as string ??
                                RazorLanguageVersionObjectJsonConverter.Instance.ReadJson(
                                    reader,
                                    objectType: null,
                                    existingValue: null,
                                    serializer) as string;
                        }
                        break;
                    case nameof(RazorConfiguration.Extensions):
                        if (reader.Read())
                        {
                            extensions = serializer.Deserialize<RazorExtension[]>(reader);
                        }
                        break;
                }
            });

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

        private class RazorLanguageVersionObjectJsonConverter : JsonConverter
        {
            public static readonly RazorLanguageVersionObjectJsonConverter Instance = new RazorLanguageVersionObjectJsonConverter();

            public override bool CanConvert(Type objectType)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType != JsonToken.StartObject)
                {
                    return null;
                }

                var major = string.Empty;
                var minor = string.Empty;

                reader.ReadProperties(propertyName =>
                {
                    switch (propertyName)
                    {
                        case "Major":
                            if (reader.Read())
                            {
                                major = reader.Value.ToString();
                            }
                            break;
                        case "Minor":
                            if (reader.Read())
                            {
                                minor = reader.Value.ToString();
                            }
                            break;
                    }
                });

                return $"{major}.{minor}";
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
