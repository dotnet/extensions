// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.Configuration
{
    public interface IConfigurationSource
    {
        bool TryGet(string key, out string value);

        void Set(string key, string value);

        void Load();

        IEnumerable<string> ProduceConfigurationSections(
            IEnumerable<string> earlierKeys,
            string parentPath,
            string delimiter);
    }
}