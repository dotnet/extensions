// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed
{
    public class OmniSharpProjectWorkspaceStateGenerator : IOmniSharpProjectSnapshotManagerChangeTrigger
    {
        // Internal for testing
        internal OmniSharpProjectWorkspaceStateGenerator()
        {
        }

        public OmniSharpProjectWorkspaceStateGenerator(OmniSharpForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            InternalWorkspaceStateGenerator = new DefaultProjectWorkspaceStateGenerator(foregroundDispatcher.InternalDispatcher);
        }

        internal DefaultProjectWorkspaceStateGenerator InternalWorkspaceStateGenerator { get; }

        public void Initialize(OmniSharpProjectSnapshotManagerBase projectManager) => InternalWorkspaceStateGenerator.Initialize(projectManager.InternalProjectSnapshotManager);

        public virtual void Update(Project workspaceProject, OmniSharpProjectSnapshot projectSnapshot) => InternalWorkspaceStateGenerator.Update(workspaceProject, projectSnapshot.InternalProjectSnapshot);
    }
}
