// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.Configuration.Ini
{
    /// <summary>
    /// An INI file based <see cref="ConfigurationProvider"/>.
    /// Files are simple line structures (<a href="https://en.wikipedia.org/wiki/INI_file">INI Files on Wikipedia</a>)
    /// </summary>
    /// <examples>
    /// [Section:Header]
    /// key1=value1
    /// key2 = " value2 "
    /// ; comment
    /// # comment
    /// / comment
    /// </examples>
    public class IniConfigurationStreamLoader : IConfigurationStreamLoader
    {
        /// <summary>
        /// Read a stream of INI values into a key/value dictionary.
        /// </summary>
        /// <param name="stream">The stream of INI data.</param>
        /// <returns></returns>
        public static IDictionary<string, string> Read(Stream stream)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = new StreamReader(stream))
            {
                var sectionPrefix = string.Empty;

                while (reader.Peek() != -1)
                {
                    var rawLine = reader.ReadLine();
                    var line = rawLine.Trim();

                    // Ignore blank lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    // Ignore comments
                    if (line[0] == ';' || line[0] == '#' || line[0] == '/')
                    {
                        continue;
                    }
                    // [Section:header] 
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        // remove the brackets
                        sectionPrefix = line.Substring(1, line.Length - 2) + ConfigurationPath.KeyDelimiter;
                        continue;
                    }

                    // key = value OR "value"
                    int separator = line.IndexOf('=');
                    if (separator < 0)
                    {
                        throw new FormatException(Resources.FormatError_UnrecognizedLineFormat(rawLine));
                    }

                    string key = sectionPrefix + line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();

                    // Remove quotes
                    if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (data.ContainsKey(key))
                    {
                        throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                    }

                    data[key] = value;
                }
            }
            return data;
        }

        /// <summary>
        /// Loads INI configuration key/values from a stream into a provider.
        /// </summary>
        /// <param name="provider">The <see cref="IConfigurationProvider"/> to store the data.</param>
        /// <param name="stream">The <see cref="Stream"/> to load ini configuration data from.</param>
        public void Load(IConfigurationProvider provider, Stream stream)
            => provider.Use(Read(stream));
    }
}
