// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    [Shared]
    [Export(typeof(ProjectWorkspaceStateGenerator))]
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    internal class DefaultProjectWorkspaceStateGenerator : ProjectWorkspaceStateGenerator, IDisposable
    {
        // Internal for testing
        internal readonly Dictionary<string, UpdateItem> _updates;

        private readonly ForegroundDispatcher _foregroundDispatcher;
        private ProjectSnapshotManagerBase _projectManager;
        private TagHelperResolver _tagHelperResolver;

        [ImportingConstructor]
        public DefaultProjectWorkspaceStateGenerator(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;

            _updates = new Dictionary<string, UpdateItem>(FilePathComparer.Instance);
        }

        // Used in unit tests to ensure we can control when background work starts.
        public ManualResetEventSlim BlockBackgroundWorkStart { get; set; }

        // Used in unit tests to ensure we can know when background work finishes.
        public ManualResetEventSlim NotifyBackgroundWorkCompleted { get; set; }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;

            _tagHelperResolver = _projectManager.Workspace.Services.GetRequiredService<TagHelperResolver>();
        }

        public override void Update(Project workspaceProject, ProjectSnapshot projectSnapshot)
        {
            if (projectSnapshot == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshot));
            }

            _foregroundDispatcher.AssertForegroundThread();

            if (_updates.TryGetValue(projectSnapshot.FilePath, out var updateItem) &&
                !updateItem.Task.IsCompleted)
            {
                updateItem.Cts.Cancel();
            }

            updateItem?.Cts.Dispose();

            var cts = new CancellationTokenSource();
            var updateTask = Task.Factory.StartNew(
                () => UpdateWorkspaceStateAsync(workspaceProject, projectSnapshot, cts.Token),
                cts.Token,
                TaskCreationOptions.None,
                _foregroundDispatcher.BackgroundScheduler).Unwrap();
            updateTask.ConfigureAwait(false);
            updateItem = new UpdateItem(updateTask, cts);
            _updates[projectSnapshot.FilePath] = updateItem;
        }

        public void Dispose()
        {
            _foregroundDispatcher.AssertForegroundThread();

            foreach (var update in _updates)
            {
                if (!update.Value.Task.IsCompleted)
                {
                    update.Value.Cts.Cancel();
                }
            }

            BlockBackgroundWorkStart?.Set();
        }

        private async Task UpdateWorkspaceStateAsync(Project workspaceProject, ProjectSnapshot projectSnapshot, CancellationToken cancellationToken)
        {
            try
            {
                _foregroundDispatcher.AssertBackgroundThread();

                OnStartingBackgroundWork();

                if (cancellationToken.IsCancellationRequested)
                {
                    // Silently cancel, we're the only ones creating these tasks.
                    return;
                }

                var workspaceState = ProjectWorkspaceState.Default;
                try
                {
                    if (workspaceProject != null)
                    {
                        var csharpLanguageVersion = LanguageVersion.Default;
                        var csharpParseOptions = (CSharpParseOptions)workspaceProject.ParseOptions;
                        if (csharpParseOptions == null)
                        {
                            Debug.Fail("Workspace project should always have CSharp parse options.");
                        }
                        else
                        {
                            csharpLanguageVersion = csharpParseOptions.LanguageVersion;
                        }
                        var tagHelperResolutionResult = await _tagHelperResolver.GetTagHelpersAsync(workspaceProject, projectSnapshot, cancellationToken);
                        workspaceState = new ProjectWorkspaceState(tagHelperResolutionResult.Descriptors, csharpLanguageVersion);
                    }
                }
                catch (Exception ex)
                {
                    await Task.Factory.StartNew(
                       () => _projectManager.ReportError(ex, projectSnapshot),
                       CancellationToken.None, // Don't allow errors to be cancelled
                       TaskCreationOptions.None,
                       _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    // Silently cancel, we're the only ones creating these tasks.
                    return;
                }

                await Task.Factory.StartNew(
                    () =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        ReportWorkspaceStateChange(projectSnapshot.FilePath, workspaceState);
                    },
                    cancellationToken,
                    TaskCreationOptions.None,
                    _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // This is something totally unexpected, let's just send it over to the project manager.
                await Task.Factory.StartNew(
                    () => _projectManager.ReportError(ex),
                    CancellationToken.None, // Don't allow errors to be cancelled
                    TaskCreationOptions.None,
                    _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
            }

            OnBackgroundWorkCompleted();
        }

        private void ReportWorkspaceStateChange(string projectFilePath, ProjectWorkspaceState workspaceStateChange)
        {
            _foregroundDispatcher.AssertForegroundThread();

            _projectManager.ProjectWorkspaceStateChanged(projectFilePath, workspaceStateChange);
        }

        private void OnStartingBackgroundWork()
        {
            if (BlockBackgroundWorkStart != null)
            {
                BlockBackgroundWorkStart.Wait();
                BlockBackgroundWorkStart.Reset();
            }
        }

        private void OnBackgroundWorkCompleted()
        {
            if (NotifyBackgroundWorkCompleted != null)
            {
                NotifyBackgroundWorkCompleted.Set();
            }
        }

        // Internal for testing
        internal class UpdateItem
        {
            public UpdateItem(Task task, CancellationTokenSource cts)
            {
                if (task == null)
                {
                    throw new ArgumentNullException(nameof(task));
                }

                if (cts == null)
                {
                    throw new ArgumentNullException(nameof(cts));
                }

                Task = task;
                Cts = cts;
            }

            public Task Task { get; }

            public CancellationTokenSource Cts { get; }
        }
    }
}