// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    internal class ProjectSnapshotSynchronizationService : ICollaborationService, IAsyncDisposable
    {
        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly CollaborationSession _sessionContext;
        private readonly IProjectSnapshotManagerProxy _hostProjectManagerProxy;
        private readonly ProjectSnapshotManagerBase _projectSnapshotManager;

        public ProjectSnapshotSynchronizationService(
            JoinableTaskFactory joinableTaskFactory,
            CollaborationSession sessionContext,
            IProjectSnapshotManagerProxy hostProjectManagerProxy,
            ProjectSnapshotManagerBase projectSnapshotManager)
        {
            if (joinableTaskFactory == null)
            {
                throw new ArgumentNullException(nameof(joinableTaskFactory));
            }

            if (sessionContext == null)
            {
                throw new ArgumentNullException(nameof(sessionContext));
            }

            if (hostProjectManagerProxy == null)
            {
                throw new ArgumentNullException(nameof(hostProjectManagerProxy));
            }

            if (projectSnapshotManager == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManager));
            }

            _joinableTaskFactory = joinableTaskFactory;
            _sessionContext = sessionContext;
            _hostProjectManagerProxy = hostProjectManagerProxy;
            _projectSnapshotManager = projectSnapshotManager;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            // We wire the changed event up early because any changed events that fire will ensure we have the most
            // up-to-date state.
            _hostProjectManagerProxy.Changed += HostProxyProjectManager_Changed;

            var projectManagerState = await _hostProjectManagerProxy.GetProjectManagerStateAsync(cancellationToken);

            await InitializeGuestProjectManagerAsync(projectManagerState.ProjectHandles, cancellationToken);
        }

        public async Task DisposeAsync()
        {
            _hostProjectManagerProxy.Changed -= HostProxyProjectManager_Changed;

            await _joinableTaskFactory.SwitchToMainThreadAsync();

            var projects = _projectSnapshotManager.Projects.ToArray();
            foreach (var project in projects)
            {
                try
                {
                    _projectSnapshotManager.ProjectRemoved(((DefaultProjectSnapshot)project).HostProject);
                }
                catch (Exception ex)
                {
                    _projectSnapshotManager.ReportError(ex, project);
                }
            }
        }

        // Internal for testing
        internal void UpdateGuestProjectManager(ProjectChangeEventProxyArgs args)
        {
            if (args.Kind == ProjectProxyChangeKind.ProjectAdded)
            {
                var guestPath = ResolveGuestPath(args.ProjectFilePath);
                var hostProject = new HostProject(guestPath, args.Newer.Configuration);
                _projectSnapshotManager.ProjectAdded(hostProject);

                if (args.Newer.ProjectWorkspaceState != null)
                {
                    _projectSnapshotManager.ProjectWorkspaceStateChanged(guestPath, args.Newer.ProjectWorkspaceState);
                }
            }
            else if (args.Kind == ProjectProxyChangeKind.ProjectRemoved)
            {
                var guestPath = ResolveGuestPath(args.ProjectFilePath);
                var hostProject = new HostProject(guestPath, args.Older.Configuration);
                _projectSnapshotManager.ProjectRemoved(hostProject);
            }
            else if (args.Kind == ProjectProxyChangeKind.ProjectChanged)
            {
                if (!args.Older.Configuration.Equals(args.Newer.Configuration))
                {
                    var guestPath = ResolveGuestPath(args.Newer.FilePath);
                    var hostProject = new HostProject(guestPath, args.Newer.Configuration);
                    _projectSnapshotManager.ProjectConfigurationChanged(hostProject);
                }
                else if (args.Older.ProjectWorkspaceState != args.Newer.ProjectWorkspaceState ||
                    args.Older.ProjectWorkspaceState?.Equals(args.Newer.ProjectWorkspaceState) == false)
                {
                    var guestPath = ResolveGuestPath(args.Newer.FilePath);
                    _projectSnapshotManager.ProjectWorkspaceStateChanged(guestPath, args.Newer.ProjectWorkspaceState);
                }
            }
        }

        private async Task InitializeGuestProjectManagerAsync(
            IReadOnlyList<ProjectSnapshotHandleProxy> projectHandles,
            CancellationToken cancellationToken)
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            foreach (var projectHandle in projectHandles)
            {
                var guestPath = ResolveGuestPath(projectHandle.FilePath);
                var hostProject = new HostProject(guestPath, projectHandle.Configuration);
                _projectSnapshotManager.ProjectAdded(hostProject);

                if (projectHandle.ProjectWorkspaceState != null)
                {
                    _projectSnapshotManager.ProjectWorkspaceStateChanged(guestPath, projectHandle.ProjectWorkspaceState);
                }
            }
        }

        private async void HostProxyProjectManager_Changed(object sender, ProjectChangeEventProxyArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            try
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync();

                UpdateGuestProjectManager(args);

            }
            catch (Exception ex)
            {
                _projectSnapshotManager.ReportError(ex);
            }
        }

        private string ResolveGuestPath(Uri filePath)
        {
            return _sessionContext.ConvertSharedUriToLocalPath(filePath);
        }
    }
}
