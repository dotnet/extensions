// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    internal class DefaultOmniSharpProjectSnapshotManager : OmniSharpProjectSnapshotManagerBase
    {
        public DefaultOmniSharpProjectSnapshotManager(ProjectSnapshotManagerBase projectSnapshotManager)
        {
            if (projectSnapshotManager == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManager));
            }

            InternalProjectSnapshotManager = projectSnapshotManager;

            InternalProjectSnapshotManager.Changed += ProjectSnapshotManager_Changed;
        }

        internal override ProjectSnapshotManagerBase InternalProjectSnapshotManager { get; }

        public override Workspace Workspace => InternalProjectSnapshotManager.Workspace;

        public override IReadOnlyList<OmniSharpProjectSnapshot> Projects => InternalProjectSnapshotManager.Projects.Select(project => OmniSharpProjectSnapshot.Convert(project)).ToList();

        public override event EventHandler<OmniSharpProjectChangeEventArgs> Changed;

        public override OmniSharpProjectSnapshot GetLoadedProject(string filePath)
        {
            var projectSnapshot = InternalProjectSnapshotManager.GetLoadedProject(filePath);
            var converted = OmniSharpProjectSnapshot.Convert(projectSnapshot);

            return converted;
        }

        public override void ProjectAdded(OmniSharpHostProject hostProject)
        {
            InternalProjectSnapshotManager.ProjectAdded(hostProject.InternalHostProject);
        }

        public override void ProjectRemoved(OmniSharpHostProject hostProject)
        {
            InternalProjectSnapshotManager.ProjectRemoved(hostProject.InternalHostProject);
        }

        public override void ProjectConfigurationChanged(OmniSharpHostProject hostProject)
        {
            InternalProjectSnapshotManager.ProjectConfigurationChanged(hostProject.InternalHostProject);
        }

        public override void ProjectWorkspaceStateChanged(string projectFilePath, ProjectWorkspaceState projectWorkspaceState)
        {
            InternalProjectSnapshotManager.ProjectWorkspaceStateChanged(projectFilePath, projectWorkspaceState);
        }

        public override void DocumentAdded(OmniSharpHostProject hostProject, OmniSharpHostDocument hostDocument)
        {
            InternalProjectSnapshotManager.DocumentAdded(hostProject.InternalHostProject, hostDocument.InternalHostDocument, textLoader: null);
        }

        public override void DocumentRemoved(OmniSharpHostProject hostProject, OmniSharpHostDocument hostDocument)
        {
            InternalProjectSnapshotManager.DocumentRemoved(hostProject.InternalHostProject, hostDocument.InternalHostDocument);
        }

        private void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            var convertedArgs = new OmniSharpProjectChangeEventArgs(args);
            Changed?.Invoke(this, convertedArgs);
        }
    }
}
