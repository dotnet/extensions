// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed.Serialization
{
    internal class OmniSharpProjectSnapshotHandleJsonConverter : JsonConverter
    {
        public static readonly OmniSharpProjectSnapshotHandleJsonConverter Instance = new OmniSharpProjectSnapshotHandleJsonConverter();

        public override bool CanConvert(Type objectType)
        {
            return typeof(OmniSharpProjectSnapshot).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var snapshot = (OmniSharpProjectSnapshot)value;

            serializer.Serialize(writer, snapshot.InternalProjectSnapshot);
        }
    }
}