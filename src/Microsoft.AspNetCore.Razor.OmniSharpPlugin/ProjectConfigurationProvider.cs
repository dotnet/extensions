// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class ProjectConfigurationProvider
    {
        public abstract bool TryResolveConfiguration(ProjectConfigurationProviderContext context, out ProjectConfiguration configuration);
    }
}
