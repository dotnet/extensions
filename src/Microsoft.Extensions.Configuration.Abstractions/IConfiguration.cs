// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    public interface IConfiguration
    {
        string this[string key] { get; set; }

        IConfigurationSection GetSection(string key);

        IEnumerable<IConfigurationSection> GetChildren();

        IChangeToken GetReloadToken();
    }
}
