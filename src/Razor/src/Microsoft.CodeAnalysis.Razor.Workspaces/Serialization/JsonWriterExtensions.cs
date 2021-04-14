// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor.Serialization
{
    internal static class JsonWriterExtensions
    {
        public static void WritePropertyArray<T>(this JsonWriter writer, string propertyName, IReadOnlyList<T> collection, JsonSerializer serializer)
        {
            writer.WritePropertyName(propertyName);
            writer.WriteStartArray();
            foreach (var item in collection)
            {
                serializer.Serialize(writer, item);
            }
            writer.WriteEndArray();
        }
    }
}
