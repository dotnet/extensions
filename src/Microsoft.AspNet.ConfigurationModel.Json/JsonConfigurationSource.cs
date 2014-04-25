using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

using Resources = Microsoft.AspNet.ConfigurationModel.Json.Resources;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class JsonConfigurationSource : BaseConfigurationSource
    {
        public JsonConfigurationSource(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException(Resources.Error_InvalidFilePath, "path");
            }

            Path = path;
        }

        public string Path { get; private set; }

        public override void Load()
        {
            using (var stream = new FileStream(Path, FileMode.Open))
            {
                Load(stream);
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
                            data[reader.Path.Replace(".", Constants.KeyDelimiter)] = reader.Value.ToString();
                            break;

                        // End of file
                        case JsonToken.None:
                            {
                                throw new FormatException(Resources.FormatError_UnexpectedEnd( reader.Path,
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

            ReplaceData(data);
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
