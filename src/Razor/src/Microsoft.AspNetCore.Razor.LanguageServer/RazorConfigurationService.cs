// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal abstract class RazorConfigurationService
    {
        public abstract Task<RazorLSPOptions> GetLatestOptionsAsync(CancellationToken cancellationToken);
    }
}
