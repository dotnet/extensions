// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed
{
    [Shared]
    [ExportWorkspaceServiceFactory(typeof(ProjectSnapshotProjectEngineFactory))]
    internal class DefaultProjectSnapshotProjectEngineFactoryFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            return new DefaultProjectSnapshotProjectEngineFactory(new FallbackProjectEngineFactory(), ProjectEngineFactories.Factories);
        }
    }
}
