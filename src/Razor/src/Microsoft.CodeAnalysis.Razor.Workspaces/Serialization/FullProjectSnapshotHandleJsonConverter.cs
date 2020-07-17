// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Serialization
{
    internal class FullProjectSnapshotHandleJsonConverter : JsonConverter
    {
        public static readonly FullProjectSnapshotHandleJsonConverter Instance = new FullProjectSnapshotHandleJsonConverter();
        private const string SerializationFormatPropertyName = "SerializationFormat";

        public override bool CanConvert(Type objectType)
        {
            return typeof(FullProjectSnapshotHandle).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            string serializationFormat = null;
            string filePath = null;
            RazorConfiguration configuration = null;
            string rootNamespace = null;
            ProjectWorkspaceState projectWorkspaceState = null;
            DocumentSnapshotHandle[] documents = null;

            reader.ReadProperties(propertyName =>
            {
                switch (propertyName)
                {
                    case SerializationFormatPropertyName:
                        if (reader.Read())
                        {
                            serializationFormat = (string)reader.Value;
                        }
                        break;
                    case nameof(FullProjectSnapshotHandle.FilePath):
                        if (reader.Read())
                        {
                            filePath = (string)reader.Value;
                        }
                        break;
                    case nameof(FullProjectSnapshotHandle.Configuration):
                        if (reader.Read())
                        {
                            configuration = RazorConfigurationJsonConverter.Instance.ReadJson(reader, objectType, existingValue, serializer) as RazorConfiguration;
                        }
                        break;
                    case nameof(FullProjectSnapshotHandle.RootNamespace):
                        if (reader.Read())
                        {
                            rootNamespace = (string)reader.Value;
                        }
                        break;
                    case nameof(FullProjectSnapshotHandle.ProjectWorkspaceState):
                        if (reader.Read())
                        {
                            projectWorkspaceState = serializer.Deserialize<ProjectWorkspaceState>(reader);
                        }
                        break;
                    case nameof(FullProjectSnapshotHandle.Documents):
                        if (reader.Read())
                        {
                            documents = serializer.Deserialize<DocumentSnapshotHandle[]>(reader);
                        }
                        break;
                }
            });

            // We need to add a serialization format to the project response to indicate that this version of the code is compatible with what's being serialized.
            // This scenario typically happens when a user has an incompatible serialized project snapshot but is using the latest Razor bits.

            if (string.IsNullOrEmpty(serializationFormat) || serializationFormat != ProjectSerializationFormat.Version)
            {
                // Unknown serialization format.
                return null;
            }

            return new FullProjectSnapshotHandle(filePath, configuration, rootNamespace, projectWorkspaceState, documents);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var handle = (FullProjectSnapshotHandle)value;

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(FullProjectSnapshotHandle.FilePath));
            writer.WriteValue(handle.FilePath);

            if (handle.Configuration == null)
            {
                writer.WritePropertyName(nameof(FullProjectSnapshotHandle.Configuration));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(FullProjectSnapshotHandle.Configuration));
                serializer.Serialize(writer, handle.Configuration);
            }

            if (handle.ProjectWorkspaceState == null)
            {
                writer.WritePropertyName(nameof(FullProjectSnapshotHandle.ProjectWorkspaceState));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(FullProjectSnapshotHandle.ProjectWorkspaceState));
                serializer.Serialize(writer, handle.ProjectWorkspaceState);
            }

            writer.WritePropertyName(nameof(FullProjectSnapshotHandle.RootNamespace));
            writer.WriteValue(handle.RootNamespace);

            writer.WritePropertyName(nameof(FullProjectSnapshotHandle.Documents));
            serializer.Serialize(writer, handle.Documents);

            writer.WritePropertyName(SerializationFormatPropertyName);
            writer.WriteValue(ProjectSerializationFormat.Version);

            writer.WriteEndObject();
        }
    }
}