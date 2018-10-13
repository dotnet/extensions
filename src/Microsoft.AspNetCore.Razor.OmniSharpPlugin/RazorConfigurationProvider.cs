// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class RazorConfigurationProvider
    {
        public abstract bool TryResolveConfiguration(RazorConfigurationProviderContext context, out RazorConfiguration configuration);
    }
}
