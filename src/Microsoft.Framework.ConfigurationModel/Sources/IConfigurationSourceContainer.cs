// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.ConfigurationModel
{
#if ASPNET50 || ASPNETCORE50
    [Microsoft.Framework.Runtime.AssemblyNeutral]
#endif
    public interface IConfigurationSourceContainer : IConfiguration, IEnumerable<IConfigurationSource>
    {
        IConfigurationSourceContainer Add(IConfigurationSource configurationSource);
    }
}