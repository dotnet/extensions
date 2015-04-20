// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.ConfigurationModel.Json;
using Newtonsoft.Json;

namespace Microsoft.Framework.ConfigurationModel
{
    /// <summary>
    /// A JSON file based <see cref="ConfigurationSource"/>.
    /// </summary>
    public class JsonConfigurationSource : ConfigurationSource
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonConfigurationSource"/>.
        /// </summary>
        /// <param name="path">Absolute path of the JSON configuration file.</param>
        public JsonConfigurationSource(string path)
            : this(path, optional: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="JsonConfigurationSource"/>.
        /// </summary>
        /// <param name="path">Absolute path of the JSON configuration file.</param>
        /// <param name="optional">Determines if the configuration is optional.</param>
        public JsonConfigurationSource(string path, bool optional)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, nameof(path));
            }

            Optional = optional;
            Path = path;
        }

        /// <summary>
        /// Gets a value that determines if this instance of <see cref="JsonConfigurationSource"/> is optional.
        /// </summary>
        public bool Optional { get; }

        /// <summary>
        /// The absolute path of the file backing this instance of <see cref="JsonConfigurationSource"/>.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Loads the contents of the file at <see cref="Path"/>.
        /// </summary>
        /// <exception cref="FileNotFoundException">If <see cref="Optional"/> is <c>false</c> and a
        /// file does not exist at <see cref="Path"/>.</exception>
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
                    throw new FileNotFoundException(Resources.FormatError_FileNotFound(Path), Path);
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
