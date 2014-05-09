// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.Framework.ConfigurationModel
{
    //[AssemblyNeutral]
    public interface IConfigurationSource
    {
        bool TryGet(string key, out string value);

        void Set(string key, string value);

        void Load();

        IEnumerable<string> ProduceSubKeys(IEnumerable<string> earlierKeys, string prefix, string delimiter);
    }
}