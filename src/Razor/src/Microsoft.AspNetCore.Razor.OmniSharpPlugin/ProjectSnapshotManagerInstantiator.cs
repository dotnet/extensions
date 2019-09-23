// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using OmniSharp.MSBuild.Notification;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(IMSBuildEventSink))]
    internal class ProjectSnapshotManagerInstantiator : IMSBuildEventSink
    {
        // The entire purpose of this class is to ensure the project manager is instantiated.
        // Without this class all exporters of IOmniSharpProjectSnapshotManagerChangeTrigger
        // would never be called (the class wouldn't have been created). So instead we rely
        // on OmniSharp to instantiate the snapshot manager and therefore configure the
        // dependent change triggers.

        private readonly OmniSharpProjectSnapshotManager _projectManager;

        [ImportingConstructor]
        public ProjectSnapshotManagerInstantiator(OmniSharpProjectSnapshotManagerAccessor accessor)
        {
            _projectManager = accessor.Instance;
        }

        public void ProjectLoaded(ProjectLoadedEventArgs _)
        {
        }
    }
}
