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
#if NET45
        public CommandLineConfigurationSource()
            : this(Environment.GetCommandLineArgs())
        {
            Args = Environment.GetCommandLineArgs();
        }
#endif

        public CommandLineConfigurationSource(string[] args)
        {
            Args = args;
        }

        public string[] Args { get; private set; }

        public override void Load()
        {
#warning TODO - this is a placeholder algorithm which must be replaced

            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pair in Args)
            {
                int split = pair.IndexOf('=');
                if (split <= 0)
                {
                    throw new FormatException(Resources.FormatError_UnrecognizedArgumentFormat(pair));
                }

                string key = pair.Substring(0, split);
                string value = pair.Substring(split + 1);
                if (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    // Remove quotes
                    value = value.Substring(1, value.Length - 2);
                }

                if (data.ContainsKey(key))
                {
                    throw new FormatException(Resources.FormatError_KeyIsDuplicated(key));
                }

                data[key] = value;
            }

            ReplaceData(data);
        }
    }
}
