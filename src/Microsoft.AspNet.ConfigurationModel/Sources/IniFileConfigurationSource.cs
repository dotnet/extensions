// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

#if NET45 || K10
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class IniFileConfigurationSource : BaseConfigurationSource, ICommitableConfigurationSource
    {
        // http://en.wikipedia.org/wiki/INI_file
        /// <summary>
        /// Files are simple line structures
        /// [Section:Header]
        /// key1=value1
        /// key2 = " value2 "
        /// ; comment
        /// # comment
        /// / comment
        /// </summary>
        /// <param name="path">The path and file name to load.</param>
        public IniFileConfigurationSource(string path)
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
        
        public virtual void Commit()
        {
            // If the config file is not found in given path
            // i.e. we don't have a template to follow when generating contents of new config file
            if (!File.Exists(Path))
            {
                var newConfigFileStream = new FileStream(Path, FileMode.CreateNew);

                try
                {
                    // Generate contents and write it to the newly created config file
                    GenerateNewConfig(newConfigFileStream);
                }
                catch
                {
                    newConfigFileStream.Dispose();

                    // The operation should be atomic because we don't want a corrupted config file
                    // So we roll back if the operation fails
                    if (File.Exists(Path))
                    {
                        File.Delete(Path);
                    }

                    // Rethrow the exception
                    throw;
                }
                finally
                {
                    newConfigFileStream.Dispose();
                }

                return;
            }

            // Because we need to read the original contents while generating new contents, the new contents are
            // cached in memory and used to overwrite original contents after we finish reading the original contents
            using (var cacheStream = new MemoryStream())
            {
                using (var inputStream = new FileStream(Path, FileMode.Open))
                {
                    Commit(inputStream, cacheStream);
                }

                // Use the cached new contents to overwrite original contents
                cacheStream.Seek(0, SeekOrigin.Begin);
                using (var outputStream = new FileStream(Path, FileMode.Truncate))
                {
                    cacheStream.CopyTo(outputStream);
                }
            }
        }

        internal void Load(Stream stream)
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
                        sectionPrefix = line.Substring(1, line.Length - 2) + ":";
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

            ReplaceData(data);
        }

        // Use the original file as a template while generating new file contents
        // to make sure the format is consistent and comments are not lost
        internal void Commit(Stream inputStream, Stream outputStream)
        {
            var processedKeys = new HashSet<string>();
            var outputWriter = new StreamWriter(outputStream);

            using (var inputReader = new StreamReader(inputStream))
            {
                var sectionPrefix = string.Empty;

                while (inputReader.Peek() != -1)
                {
                    var rawLine = inputReader.ReadLine();
                    var line = rawLine.Trim();

                    // Is this the last line?
                    var lineEnd = inputReader.Peek() == -1 ? string.Empty : Environment.NewLine;

                    // Ignore blank lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        outputWriter.Write(rawLine + lineEnd);
                        continue;
                    }
                    // Ignore comments
                    if (line[0] == ';' || line[0] == '#' || line[0] == '/')
                    {
                        outputWriter.Write(rawLine + lineEnd);
                        continue;
                    }
                    // [Section:header] 
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        outputWriter.Write(rawLine + lineEnd);

                        // remove the brackets
                        sectionPrefix = line.Substring(1, line.Length - 2) + ":";
                        continue;
                    }

                    // key = value OR "value"
                    int separator = line.IndexOf('=');
                    if (separator < 0)
                    {
                        throw new FormatException(Resources.FormatError_UnrecognizedLineFormat(rawLine));
                    }

                    var key = sectionPrefix + line.Substring(0, separator).Trim();
                    var value = line.Substring(separator + 1).Trim();

                    // Output preserves white spaces in original file
                    int rawSeparator = rawLine.IndexOf('=');
                    var outKeyStr = rawLine.Substring(0, rawSeparator);
                    var outValueStr = rawLine.Substring(rawSeparator + 1);

                    // Remove quotes
                    if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (!Data.ContainsKey(key))
                    {
                        throw new InvalidOperationException(Resources.FormatError_CommitWhenNewKeyFound(key));
                    }

                    outValueStr = outValueStr.Replace(value, Data[key]);

                    outputWriter.Write(string.Format("{0}={1}{2}", outKeyStr, outValueStr, lineEnd));

                    processedKeys.Add(key);
                }

                outputWriter.Flush();
            }

            if (Data.Count() != processedKeys.Count())
            {
                var missingKeys = string.Join(", ", Data.Keys.Except(processedKeys));
                throw new InvalidOperationException(Resources.FormatError_CommitWhenKeyMissing(missingKeys));
            }
        }

        // Write the contents of newly created config file to given stream
        internal void GenerateNewConfig(Stream outputStream)
        {
            var outputWriter = new StreamWriter(outputStream);

            foreach (var entry in Data)
            {
                outputWriter.WriteLine(string.Format("{0}={1}", entry.Key, entry.Value));
            }

            outputWriter.Flush();
        }
    }
}
#endif
