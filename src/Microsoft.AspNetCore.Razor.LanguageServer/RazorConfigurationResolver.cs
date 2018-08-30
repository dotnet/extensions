// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class RazorConfigurationResolver
    {
        public abstract RazorConfiguration Default { get; }

        public abstract bool TryResolve(string configurationName, out RazorConfiguration configuration);
    }
}
