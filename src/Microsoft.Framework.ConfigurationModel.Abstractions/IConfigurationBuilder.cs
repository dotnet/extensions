// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.ConfigurationModel
{
    public interface IConfigurationBuilder : IConfiguration
    {
        string BasePath { get; }

        IEnumerable<IConfigurationSource> Sources { get; }

        IConfigurationBuilder Add(IConfigurationSource configurationSource);

        void Reload();
    }
}