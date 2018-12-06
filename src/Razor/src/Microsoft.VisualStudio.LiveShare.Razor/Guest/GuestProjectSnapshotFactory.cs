// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    internal class GuestProjectSnapshotFactory : ProjectSnapshotFactory
    {
        private readonly Workspace _workspace;
        private readonly LiveShareClientProvider _liveShareClientProvider;

        public GuestProjectSnapshotFactory(
            Workspace workspace,
            LiveShareClientProvider liveShareClientProvider)
        {
            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (liveShareClientProvider == null)
            {
                throw new ArgumentNullException(nameof(liveShareClientProvider));
            }

            _workspace = workspace;
            _liveShareClientProvider = liveShareClientProvider;
        }

        public override ProjectSnapshot Create(ProjectSnapshotHandleProxy projectHandle)
        {
            if (projectHandle == null)
            {
                throw new ArgumentNullException(nameof(projectHandle));
            }

            var filePath = _liveShareClientProvider.ConvertToLocalPath(projectHandle.FilePath);
            var hostProject = new HostProject(filePath, projectHandle.Configuration);
            var projectState = ProjectState.Create(_workspace.Services, hostProject);
            var snapshot = new GuestProjectSnapshot(projectState, projectHandle.TagHelpers);
            return snapshot;
        }
    }
}
