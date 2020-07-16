// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.CodeAnalysis.Razor.Serialization
{
    internal static class JsonReaderExtensions
    {
        public static bool ReadTokenAndAdvance(this JsonReader reader, JsonToken expectedTokenType, out object value)
        {
            value = reader.Value;
            return reader.TokenType == expectedTokenType && reader.Read();
        }

        public static void ReadProperties(this JsonReader reader, Action<string> onProperty)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var propertyName = reader.Value.ToString();
                        onProperty(propertyName);
                        break;
                    case JsonToken.EndObject:
                        return;
                }
            }
        }

        public static string ReadNextStringProperty(this JsonReader reader, string propertyName)
        {
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        Debug.Assert(reader.Value.ToString() == propertyName);
                        if (reader.Read())
                        {
                            var value = (string)reader.Value;
                            return value;
                        }
                        else
                        {
                            return null;
                        }
                }
            }

            throw new JsonSerializationException($"Could not find string property '{propertyName}'.");
        }

        public static IReadOnlyList<T> ReadPropertyArray<T>(this JsonReader reader, JsonSerializer serializer, string propertyName)
            where T : class
        {
            do
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        Debug.Assert(reader.Value.ToString() == propertyName);
                        if (reader.Read())
                        {
                            return reader.ReadArray<T>(serializer);
                        }
                        else
                        {
                            return Array.Empty<T>();
                        }
                }
            } while (reader.Read());

            throw new JsonSerializationException($"Could not find array property '{propertyName}'.");
        }

        public static IReadOnlyList<T> ReadArray<T>(this JsonReader reader, JsonSerializer serializer)
            where T : class
        {
            // Ensure we're at the start of an array
            if (!reader.ReadTokenAndAdvance(JsonToken.StartArray, out _))
            {
                throw new JsonSerializationException($"Invalid array structure, missing StartArray token. Got '{reader.TokenType}'.");
            }

            var results = new List<T>();

            do
            {
                var result = serializer.Deserialize<T>(reader);

                if (result != null)
                {
                    results.Add(result);
                }

                if (reader.TokenType == JsonToken.EndObject)
                {
                    reader.Read();
                }
            } while (reader.TokenType != JsonToken.EndArray);

            reader.ReadTokenAndAdvance(JsonToken.EndArray, out _);

            return results;
        }
    }
}
