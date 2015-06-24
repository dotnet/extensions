// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Framework.Configuration.Json;
using Newtonsoft.Json;

namespace Microsoft.Framework.Configuration
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
            JsonConfigurationFileParser parser = new JsonConfigurationFileParser();
            try
            {
                Data = parser.Parse(stream);
            }
            catch(JsonReaderException e)
            {
                string errorLine = string.Empty;
                if (File.Exists(Path))
                {
                    // Read the JSON file and get the line content where the error occurred.
                    List<string> fileContent;
                    if (e.LineNumber > 1)
                    {
                        fileContent = File.ReadLines(Path).Skip(e.LineNumber - 2).Take(2).ToList();
                        errorLine = fileContent[0].Trim() + Environment.NewLine + fileContent[1].Trim();
                    }
                    else
                    {
                        fileContent = File.ReadLines(Path).Skip(e.LineNumber - 1).Take(1).ToList();
                        errorLine = fileContent[0].Trim();
                    }
                }

                throw new FormatException(Resources.FormatError_JSONParseError(e.LineNumber, errorLine), e);
            }
        }
    }
}
