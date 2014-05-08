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

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.ConfigurationModel
{
    public class CommandLineConfigurationSource : BaseConfigurationSource
    {
        private readonly Dictionary<string, string> _switchMappings;

#if NET45
        public CommandLineConfigurationSource(IDictionary<string, string> switchMappings = null)
            : this(Environment.GetCommandLineArgs())
        {
            if (switchMappings != null)
            {
                _switchMappings = GetValidatedSwitchMappingsCopy(switchMappings);
            }
        }
#endif

        public CommandLineConfigurationSource(string[] args, IDictionary<string, string> switchMappings = null)
        {
            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            Args = args;

            if (switchMappings != null)
            {
                _switchMappings = GetValidatedSwitchMappingsCopy(switchMappings);
            }
        }

        public string[] Args { get; private set; }

        public override void Load()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string key, value;
            var argIndex = 0;

            while (argIndex < Args.Length)
            {
                var currentArg = Args[argIndex];
                var keyStartIndex = 0;

                if (currentArg.StartsWith("--"))
                {
                    keyStartIndex = 2;
                }
                else if (currentArg.StartsWith("-"))
                {
                    keyStartIndex = 1;
                }
                else if (currentArg.StartsWith("/"))
                {
                    // "/SomeSwitch" is equivalent to "--SomeSwitch" when interpreting switch mappings
                    // So we do a conversion to simplify later processing
                    currentArg = string.Format("--{0}", currentArg.Substring(1));
                    keyStartIndex = 2;
                }

                var separator = currentArg.IndexOf('=');

                if (separator < 0)
                {
                    // If there is neither equal sign nor prefix in current arugment, it is an invalid format
                    if (keyStartIndex == 0)
                    {
                        throw new FormatException(Resources.FormatError_UnrecognizedArgumentFormat(currentArg));
                    }

                    // If the switch is a key in given switch mappings, interpret it
                    if (_switchMappings != null && _switchMappings.ContainsKey(currentArg))
                    {
                        key = _switchMappings[currentArg];
                    }
                    // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage
                    else if (keyStartIndex == 1)
                    {
                        throw new FormatException(Resources.FormatError_ShortSwitchNotDefined(currentArg));
                    }
                    // Otherwise, use the switch name directly as a key
                    else
                    {
                        key = currentArg.Substring(keyStartIndex);
                    }

                    argIndex++;

                    if (argIndex == Args.Length)
                    {
                        throw new FormatException(Resources.FormatError_ValueIsMissing(Args[argIndex - 1]));
                    }

                    value = Args[argIndex];
                }
                else
                {
                    var keySegment = currentArg.Substring(0, separator);

                    // If the switch is a key in given switch mappings, interpret it
                    if (_switchMappings != null && _switchMappings.ContainsKey(keySegment))
                    {
                        key = _switchMappings[keySegment];
                    }
                    // If the switch starts with a single "-" and it isn't in given mappings , it is an invalid usage
                    else if (keyStartIndex == 1)
                    {
                        throw new FormatException(Resources.FormatError_ShortSwitchNotDefined(currentArg));
                    }
                    // Otherwise, use the switch name directly as a key
                    else
                    {
                        key = currentArg.Substring(keyStartIndex, separator - keyStartIndex);
                    }

                    value = currentArg.Substring(separator + 1);
                }

                if (data.ContainsKey(key))
                {
                    throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                }

                data[key] = value;
                argIndex++;
            }

            ReplaceData(data);
        }

        private Dictionary<string, string> GetValidatedSwitchMappingsCopy(IDictionary<string, string> switchMappings)
        {
            // The dictionary passed in might be constructed with a case-sensitive comparer
            // However, the keys in configuration sources are all case-insensitive
            // So we check whether the given switch mappings contain duplicated keys with case-insensitive comparer
            var switchMappingsCopy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in switchMappings)
            {
                // Only keys start with "--" or "-" are acceptable
                if (!mapping.Key.StartsWith("--") && !mapping.Key.StartsWith("-"))
                {
                    throw new ArgumentException(Resources.FormatError_InvalidSwitchMapping(mapping.Key),
                        "switchMappings");
                }

                if (switchMappingsCopy.ContainsKey(mapping.Key))
                {
                    throw new ArgumentException(Resources.FormatError_DuplicatedKeyInSwitchMappings(mapping.Key),
                        "switchMappings");
                }

                switchMappingsCopy.Add(mapping.Key, mapping.Value);
            }

            return switchMappingsCopy;
        }
    }
}
