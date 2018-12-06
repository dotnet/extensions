// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    internal class GuestProjectSnapshotManager : ProjectSnapshotManager
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly HostWorkspaceServices _services;
        private readonly ProjectSnapshotHandleStore _projectSnapshotHandleStore;
        private readonly ProjectSnapshotFactory _projectSnapshotFactory;
        private readonly LiveShareClientProvider _liveShareClientProvider;
        private readonly Workspace _workspace;
        private Dictionary<string, ProjectSnapshot> _projects;

        public GuestProjectSnapshotManager(
            ForegroundDispatcher foregroundDispatcher,
            HostWorkspaceServices services,
            ProjectSnapshotHandleStore projectSnapshotStore,
            ProjectSnapshotFactory projectSnapshotFactory,
            LiveShareClientProvider liveShareClientProvider,
            Workspace workspace)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (projectSnapshotStore == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotStore));
            }

            if (projectSnapshotFactory == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotFactory));
            }

            if (liveShareClientProvider == null)
            {
                throw new ArgumentNullException(nameof(liveShareClientProvider));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _services = services;
            _projectSnapshotHandleStore = projectSnapshotStore;
            _projectSnapshotFactory = projectSnapshotFactory;
            _liveShareClientProvider = liveShareClientProvider;
            _workspace = workspace;
            _projectSnapshotHandleStore.Changed += ProjectSnapshotStore_Changed;
        }

        // Internal for testing
        internal GuestProjectSnapshotManager(
            ForegroundDispatcher foregroundDispatcher,
            HostWorkspaceServices services,
            ProjectSnapshotHandleStore projectSnapshotHandleStore,
            ProjectSnapshotFactory projectSnapshotFactory,
            Workspace workspace)
        {
            _foregroundDispatcher = foregroundDispatcher;
            _services = services;
            _projectSnapshotHandleStore = projectSnapshotHandleStore;
            _projectSnapshotFactory = projectSnapshotFactory;
            _workspace = workspace;
            _projectSnapshotHandleStore.Changed += ProjectSnapshotStore_Changed;
        }

        public override IReadOnlyList<ProjectSnapshot> Projects
        {
            get
            {
                _foregroundDispatcher.AssertForegroundThread();

                EnsureProjects();

                return _projects.Values.ToList();
            }
        }

        public override event EventHandler<ProjectChangeEventArgs> Changed;

        public override ProjectSnapshot GetLoadedProject(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _foregroundDispatcher.AssertForegroundThread();

            EnsureProjects();

            if (_projects.TryGetValue(filePath, out var snapshot))
            {
                return snapshot;
            }

            return null;
        }

        public override ProjectSnapshot GetOrCreateProject(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _foregroundDispatcher.AssertForegroundThread();

            EnsureProjects();

            return GetLoadedProject(filePath) ?? new EphemeralProjectSnapshot(_services, filePath);
        }

        public override bool IsDocumentOpen(string documentFilePath)
        {
            if (documentFilePath == null)
            {
                throw new ArgumentNullException(nameof(documentFilePath));
            }

            _foregroundDispatcher.AssertForegroundThread();

            // On the guest side we don't currently support open document tracking.
            return false;
        }

        // Internal for testing
        internal void ProjectSnapshotStore_Changed(object sender, ProjectProxyChangeEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _foregroundDispatcher.AssertForegroundThread();

            var guestPath = ResolveGuestPath(args.ProjectFilePath);
            var oldProject = GetLoadedProject(guestPath);

            // Reset projects cache so they'll be re-calculated.
            _projects = null;

            var newProject = GetLoadedProject(guestPath);
            var changeArgs = new ProjectChangeEventArgs(oldProject, newProject, (ProjectChangeKind)args.Kind);

            OnChanged(changeArgs);
        }

        private void OnChanged(ProjectChangeEventArgs args)
        {
            _foregroundDispatcher.AssertForegroundThread();

            Changed?.Invoke(this, args);
        }

        private void EnsureProjects()
        {
            _foregroundDispatcher.AssertForegroundThread();

            if (_projects == null)
            {
                UpdateProjects();
            }
        }

        private void UpdateProjects()
        {
            _foregroundDispatcher.AssertForegroundThread();

            var projectHandles = _projectSnapshotHandleStore.GetProjectHandles();

            _projects = projectHandles
                .Select(_projectSnapshotFactory.Create)
                .ToDictionary(project => project.FilePath, FilePathComparer.Instance);
        }

        // Internal virtual for testing
        internal virtual string ResolveGuestPath(Uri filePath)
        {
            var guestPath = _liveShareClientProvider.ConvertToLocalPath(filePath);
            return guestPath;
        }
    }
}
