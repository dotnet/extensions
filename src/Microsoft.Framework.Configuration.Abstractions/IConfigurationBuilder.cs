// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.Configuration
{
    public interface IConfigurationBuilder
    {
        Dictionary<string, object> Properties { get; }

        IEnumerable<IConfigurationProvider> Providers { get; }

        IConfigurationBuilder Add(IConfigurationProvider provider);

        IConfigurationRoot Build();
    }
}