// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Serialization
{
    internal class ProjectSnapshotHandleJsonConverter : JsonConverter
    {
        public static readonly ProjectSnapshotHandleJsonConverter Instance = new ProjectSnapshotHandleJsonConverter();

        public override bool CanConvert(Type objectType)
        {
            return typeof(ProjectSnapshotHandle).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            string filePath = null;
            RazorConfiguration configuration = null;
            string rootNamespace = null;

            reader.ReadProperties(propertyName =>
            {
                switch (propertyName)
                {
                    case nameof(ProjectSnapshotHandle.FilePath):
                        if (reader.Read())
                        {
                            filePath = (string)reader.Value;
                        }
                        break;
                    case nameof(ProjectSnapshotHandle.Configuration):
                        if (reader.Read())
                        {
                            configuration = RazorConfigurationJsonConverter.Instance.ReadJson(reader, objectType, existingValue, serializer) as RazorConfiguration;
                        }
                        break;
                    case nameof(ProjectSnapshotHandle.RootNamespace):
                        if (reader.Read())
                        {
                            rootNamespace = (string)reader.Value;
                        }
                        break;
                }
            });

            return new ProjectSnapshotHandle(filePath, configuration, rootNamespace);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var handle = (ProjectSnapshotHandle)value;

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(ProjectSnapshotHandle.FilePath));
            writer.WriteValue(handle.FilePath);

            if (handle.Configuration == null)
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandle.Configuration));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandle.Configuration));
                serializer.Serialize(writer, handle.Configuration);
            }

            if (handle.RootNamespace == null)
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandle.RootNamespace));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandle.RootNamespace));
                writer.WriteValue(handle.RootNamespace);
            }

            writer.WriteEndObject();
        }
    }
}
