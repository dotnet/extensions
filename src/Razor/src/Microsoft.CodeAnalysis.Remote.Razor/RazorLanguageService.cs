// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal class RazorLanguageService : RazorServiceBase
    {
        public RazorLanguageService(Stream stream, IServiceProvider serviceProvider)
            : base(stream, serviceProvider)
        {
        }

        public async Task<TagHelperResolutionResult> GetTagHelpersAsync(ProjectSnapshotHandle projectHandle, string factoryTypeName, CancellationToken cancellationToken = default)
        {
            var projectSnapshot = await GetProjectSnapshotAsync(projectHandle, cancellationToken).ConfigureAwait(false);
            var solution = await GetSolutionAsync(cancellationToken);
            var workspaceProject = solution
                .Projects
                .FirstOrDefault(project => FilePathComparer.Instance.Equals(project.FilePath, projectSnapshot.FilePath));

            if (workspaceProject == null)
            {
                return TagHelperResolutionResult.Empty;
            }

            return await RazorServices.TagHelperResolver.GetTagHelpersAsync(workspaceProject, projectHandle.Configuration, factoryTypeName, cancellationToken);
        }
    }
}
