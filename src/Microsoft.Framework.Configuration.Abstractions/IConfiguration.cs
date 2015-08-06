// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.Configuration
{
    public interface IConfiguration
    {
        string this[string key] { get; set; }

        IConfigurationSection GetSection(string key);

        IEnumerable<IConfigurationSection> GetChildren();
    }
}
