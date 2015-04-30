// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.ConfigurationModel
{
    public interface IConfigurationSourceRoot : IConfiguration
    {
        string BasePath { get; }

        IEnumerable<IConfigurationSource> Sources { get; }

        IConfigurationSourceRoot Add(IConfigurationSource configurationSource);

        void Reload();
    }
}