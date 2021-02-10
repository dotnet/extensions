// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed;
using Microsoft.CodeAnalysis;
using OmniSharp;
using OmniSharp.MSBuild.Notification;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(IMSBuildEventSink))]
    [Export(typeof(IRazorDocumentChangeListener))]
    [Export(typeof(IRazorDocumentOutputChangeListener))]
    [Export(typeof(IOmniSharpProjectSnapshotManagerChangeTrigger))]
    internal class TagHelperRefreshTrigger : IMSBuildEventSink, IRazorDocumentOutputChangeListener, IOmniSharpProjectSnapshotManagerChangeTrigger, IRazorDocumentChangeListener
    {
        private readonly OmniSharpForegroundDispatcher _foregroundDispatcher;
        private readonly Workspace _omniSharpWorkspace;
        private readonly OmniSharpProjectWorkspaceStateGenerator _workspaceStateGenerator;
        private readonly Dictionary<string, Task> _deferredUpdates;
        private OmniSharpProjectSnapshotManager _projectManager;

        [ImportingConstructor]
        public TagHelperRefreshTrigger(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            OmniSharpWorkspace omniSharpWorkspace,
            OmniSharpProjectWorkspaceStateGenerator workspaceStateGenerator) :
                this(foregroundDispatcher, (Workspace)omniSharpWorkspace, workspaceStateGenerator)
        {
        }

        // Internal for testing
        internal TagHelperRefreshTrigger(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            Workspace omniSharpWorkspace,
            OmniSharpProjectWorkspaceStateGenerator workspaceStateGenerator)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (omniSharpWorkspace == null)
            {
                throw new ArgumentNullException(nameof(omniSharpWorkspace));
            }

            if (workspaceStateGenerator == null)
            {
                throw new ArgumentNullException(nameof(workspaceStateGenerator));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _omniSharpWorkspace = omniSharpWorkspace;
            _workspaceStateGenerator = workspaceStateGenerator;
            _deferredUpdates = new Dictionary<string, Task>();
        }

        public int EnqueueDelay { get; set; } = 3 * 1000;

        public void Initialize(OmniSharpProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
        }

        public void ProjectLoaded(ProjectLoadedEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Project file was modified or impacted in a significant way.

            _ = Task.Factory.StartNew(
                () => EnqueueUpdate(args.ProjectInstance.ProjectFileLocation.File),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
        }

        public void RazorDocumentChanged(RazorFileChangeEventArgs args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Razor document changed

            _ = Task.Factory.StartNew(
                () =>
                {
                    if (IsComponentFile(args.FilePath, args.UnevaluatedProjectInstance.ProjectFileLocation.File))
                    {
                        // Razor component file changed.

                        EnqueueUpdate(args.UnevaluatedProjectInstance.ProjectFileLocation.File);
                    }
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
        }

        public void RazorDocumentOutputChanged(RazorFileChangeEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Razor build occurred

            _ = Task.Factory.StartNew(
                () => EnqueueUpdate(args.UnevaluatedProjectInstance.ProjectFileLocation.File),
                CancellationToken.None,
                TaskCreationOptions.None,
                _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
        }

        // Internal for testing
        internal async Task UpdateAfterDelayAsync(string projectFilePath)
        {
            if (string.IsNullOrEmpty(projectFilePath))
            {
                return;
            }

            await Task.Delay(EnqueueDelay);

            var solution = _omniSharpWorkspace.CurrentSolution;
            var workspaceProject = solution.Projects.FirstOrDefault(project => FilePathComparer.Instance.Equals(project.FilePath, projectFilePath));
            if (workspaceProject != null && TryGetProjectSnapshot(workspaceProject.FilePath, out var projectSnapshot))
            {
                _workspaceStateGenerator.Update(workspaceProject, projectSnapshot);
            }
        }

        private void EnqueueUpdate(string projectFilePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            // A race is not possible here because we use the main thread to synchronize the updates
            // by capturing the sync context.
            if (!_deferredUpdates.TryGetValue(projectFilePath, out var update) || update.IsCompleted)
            {
                _deferredUpdates[projectFilePath] = UpdateAfterDelayAsync(projectFilePath);
            }
        }

        private bool TryGetProjectSnapshot(string projectFilePath, out OmniSharpProjectSnapshot projectSnapshot)
        {
            if (projectFilePath == null)
            {
                projectSnapshot = null;
                return false;
            }

            projectSnapshot = _projectManager.GetLoadedProject(projectFilePath);
            return projectSnapshot != null;
        }

        // Internal for testing
        internal bool IsComponentFile(string relativeDocumentFilePath, string projectFilePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var projectSnapshot = _projectManager.GetLoadedProject(projectFilePath);
            if (projectSnapshot == null)
            {
                return false;
            }

            var documentSnapshot = projectSnapshot.GetDocument(relativeDocumentFilePath);
            if (documentSnapshot == null)
            {
                return false;
            }

            var isComponentKind = FileKinds.IsComponent(documentSnapshot.FileKind);
            return isComponentKind;
        }
    }
}
