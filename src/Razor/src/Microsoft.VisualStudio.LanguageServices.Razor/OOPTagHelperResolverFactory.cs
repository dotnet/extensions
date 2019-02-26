// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    [System.Composition.Shared]
    [ExportWorkspaceServiceFactory(typeof(TagHelperResolver), ServiceLayer.Host)]
    internal class OOPTagHelperResolverFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new OOPTagHelperResolver(
                workspaceServices.GetRequiredService<ProjectSnapshotProjectEngineFactory>(),
                workspaceServices.GetRequiredService<ErrorReporter>(),
                workspaceServices.Workspace);
        }
    }
}