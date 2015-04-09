// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Resources = Microsoft.Framework.ConfigurationModel.Json.Resources;

namespace Microsoft.Framework.ConfigurationModel
{
    public class JsonConfigurationSource : ConfigurationSource
    {
        public JsonConfigurationSource(string path)
            : this(path, optional: false)
        {
        }

        public JsonConfigurationSource(string path, bool optional)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, "path");
            }

            Optional = optional;
            Path = path;
        }

        public bool Optional { get; private set; }

        public string Path { get; private set; }

        public override void Load()
        {
            if (!File.Exists(Path))
            {
                if (Optional)
                {
                    Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    throw new FileNotFoundException(string.Format(Resources.Error_FileNotFound, Path), Path);
                }
            }
            else
            {
                using (var stream = new FileStream(Path, FileMode.Open, FileAccess.Read))
                {
                    Load(stream);
                }
            }
        }

        internal void Load(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                var startObjectCount = 0;

                // Dates are parsed as strings
                reader.DateParseHandling = DateParseHandling.None;

                // Move to the first token
                reader.Read();

                SkipComments(reader);

                if (reader.TokenType != JsonToken.StartObject)
                {
                    throw new FormatException(Resources.FormatError_RootMustBeAnObject(reader.Path,
                        reader.LineNumber, reader.LinePosition));
                }

                do
                {
                    SkipComments(reader);

                    switch (reader.TokenType)
                    {
                        case JsonToken.StartObject:
                            startObjectCount++;
                            break;

                        case JsonToken.EndObject:
                            startObjectCount--;
                            break;

                        // Keys in key-value pairs
                        case JsonToken.PropertyName:
                            break;

                        // Values in key-value pairs
                        case JsonToken.Integer:
                        case JsonToken.Float:
                        case JsonToken.String:
                        case JsonToken.Boolean:
                        case JsonToken.Bytes:
                        case JsonToken.Raw:
                        case JsonToken.Null:
                            var key = GetKey(reader.Path);

                            if (data.ContainsKey(key))
                            {
                                throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                            }
                            data[key] = reader.Value.ToString();
                            break;

                        // End of file
                        case JsonToken.None:
                            {
                                throw new FormatException(Resources.FormatError_UnexpectedEnd(reader.Path,
                                    reader.LineNumber, reader.LinePosition));
                            }

                        default:
                            {
                                // Unsupported elements: Array, Constructor, Undefined
                                throw new FormatException(Resources.FormatError_UnsupportedJSONToken(
                                    reader.TokenType, reader.Path, reader.LineNumber, reader.LinePosition));
                            }
                    }

                    reader.Read();

                } while (startObjectCount > 0);
            }

            Data = data;
        }

        private string GetKey(string jsonPath)
        {
            var pathSegments = new List<string>();
            var index = 0;

            while (index < jsonPath.Length)
            {
                // If the JSON element contains '.' in its name, JSON.net escapes that element as ['element']
                // while getting its Path. So before replacing '.' => ':' to represent JSON hierarchy, here 
                // we skip a '.' => ':' conversion if the element is not enclosed with in ['..'].
                var start = jsonPath.IndexOf("['", index);

                if (start < 0)
                {
                    // No more ['. Skip till end of string.
                    pathSegments.Add(jsonPath.
                        Substring(index).
                        Replace('.', ':'));
                    break;
                }
                else
                {
                    if (start > index)
                    {
                        pathSegments.Add(
                            jsonPath
                            .Substring(index, start - index) // Anything between the previous [' and '].
                            .Replace('.', ':'));
                    }

                    var endIndex = jsonPath.IndexOf("']", start);
                    pathSegments.Add(jsonPath.Substring(start + 2, endIndex - start - 2));
                    index = endIndex + 2;
                }
            }

            return string.Join(string.Empty, pathSegments);
        }

        private void SkipComments(JsonReader reader)
        {
            while (reader.TokenType == JsonToken.Comment)
            {
                reader.Read();
            }
        }
    }
}
