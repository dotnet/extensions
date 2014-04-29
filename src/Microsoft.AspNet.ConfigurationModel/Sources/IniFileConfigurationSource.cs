using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNet.ConfigurationModel.Sources
{
    public class IniFileConfigurationSource : BaseConfigurationSource, ICommitableConfigurationSource
    {
        private bool _loaded;
        
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
            _loaded = false;
        }

        public string Path { get; private set; }

        public override void Load()
        {
            using (var stream = new FileStream(Path, FileMode.Open))
            {
                Load(stream);
            }
            
            _loaded = true;
        }
        
        public virtual void Commit()
        {
            if (!_loaded)
            {
                throw new InvalidOperationException(Resources.Error_CommitWhenNotLoaded);
            }

            if (!File.Exists(Path))
            {
                throw new InvalidOperationException(Resources.Error_CommitWhenFileNotExist);
            }

            Stream newContentsStream = null;

            using (var stream = new FileStream(Path, FileMode.Open))
            {
                newContentsStream = Commit(stream);
            }

            using (var stream = new FileStream(Path, FileMode.Truncate))
            {
                newContentsStream.CopyTo(stream);
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

        // Parse the original file while generating new file contents
        // to make sure the format is consistent and comments are not lost
        internal Stream Commit(Stream configFileStream)
        {
            var newContentsStream = new MemoryStream();
            var newContentsWriter = new StreamWriter(newContentsStream);

            using (var configFileReader = new StreamReader(configFileStream))
            {
                var sectionPrefix = string.Empty;

                while (configFileReader.Peek() != -1)
                {
                    var rawLine = configFileReader.ReadLine();
                    var line = rawLine.Trim();

                    // Is this the last line?
                    var lineEnd = configFileReader.Peek() == -1 ? string.Empty : Environment.NewLine;

                    // Ignore blank lines
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        newContentsWriter.Write(rawLine + lineEnd);
                        continue;
                    }
                    // Ignore comments
                    if (line[0] == ';' || line[0] == '#' || line[0] == '/')
                    {
                        newContentsWriter.Write(rawLine + lineEnd);
                        continue;
                    }
                    // [Section:header] 
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        newContentsWriter.Write(rawLine + lineEnd);

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

                    newContentsWriter.Write(string.Format("{0}={1}{2}", outKeyStr, outValueStr, lineEnd));

                    Data.Remove(key);
                }
            }

            if (Data.Any())
            {
                var missingKeys = string.Join(" ", Data.Keys);
                throw new InvalidOperationException(Resources.FormatError_CommitWhenKeyMissing(missingKeys));
            }

            newContentsWriter.Flush();
            newContentsStream.Seek(0, SeekOrigin.Begin);
            return newContentsStream;
        }
    }
}
