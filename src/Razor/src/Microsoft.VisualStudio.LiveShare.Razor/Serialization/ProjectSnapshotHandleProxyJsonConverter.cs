// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LiveShare.Razor.Serialization
{
    internal class ProjectSnapshotHandleProxyJsonConverter : JsonConverter
    {
        public static readonly ProjectSnapshotHandleProxyJsonConverter Instance = new ProjectSnapshotHandleProxyJsonConverter();

        public override bool CanConvert(Type objectType)
        {
            return typeof(ProjectSnapshotHandleProxy).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return null;
            }

            var obj = JObject.Load(reader);
            var filePath = obj[nameof(ProjectSnapshotHandleProxy.FilePath)].ToObject<Uri>(serializer);
            var tagHelpers = obj[nameof(ProjectSnapshotHandleProxy.TagHelpers)].ToObject<List<TagHelperDescriptor>>(serializer);
            var configuration = obj[nameof(ProjectSnapshotHandleProxy.Configuration)].ToObject<RazorConfiguration>(serializer);

            return new ProjectSnapshotHandleProxy(filePath, tagHelpers, configuration);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var handle = (ProjectSnapshotHandleProxy)value;

            writer.WriteStartObject();

            writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.FilePath));
            writer.WriteValue(handle.FilePath);

            writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.TagHelpers));
            serializer.Serialize(writer, handle.TagHelpers);

            if (handle.Configuration == null)
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.Configuration));
                writer.WriteNull();
            }
            else
            {
                writer.WritePropertyName(nameof(ProjectSnapshotHandleProxy.Configuration));
                serializer.Serialize(writer, handle.Configuration);
            }

            writer.WriteEndObject();
        }
    }
}
