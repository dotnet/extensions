// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    internal abstract class TagHelperResolver
    {
        public abstract Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync(
            Project project,
            RazorProjectEngine engine,
            CancellationToken cancellationToken);
    }
}
