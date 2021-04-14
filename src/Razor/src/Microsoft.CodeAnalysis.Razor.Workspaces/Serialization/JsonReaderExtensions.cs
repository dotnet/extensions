// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            do
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
            } while (reader.Read());
        }

        public static bool TryReadNextProperty<TReturn>(this JsonReader reader, string propertyName, out TReturn value)
        {
            do
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        // Ensures we're at the expected property & the reader
                        // can read the property value.
                        if (reader.Value.ToString() == propertyName &&
                            reader.Read())
                        {
                            value = (TReturn)reader.Value;
                            return true;
                        }
                        else
                        {
                            value = default;
                            return false;
                        }
                }
            } while (reader.Read());

            throw new JsonSerializationException($"Could not find string property '{propertyName}'.");
        }
    }
}
