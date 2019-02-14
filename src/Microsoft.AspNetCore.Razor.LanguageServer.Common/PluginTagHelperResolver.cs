// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    // We implement the class here in the common assembly so the OmniSharpPlugin can load this assembly and reflectively invoke
    // this TagHelperResolver.
    internal class PluginTagHelperResolver : TagHelperResolver
    {
        public override Task<TagHelperResolutionResult> GetTagHelpersAsync(
            Project workspaceProject,
            ProjectSnapshot projectSnapshot,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync(
            Project project,
            RazorProjectEngine engine,
            CancellationToken cancellationToken)
        {
            var result = await GetTagHelpersAsync(project, engine);

            return result.Descriptors;
        }
    }
}
