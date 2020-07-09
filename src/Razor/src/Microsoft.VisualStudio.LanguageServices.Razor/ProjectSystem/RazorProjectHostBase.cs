// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    /// <summary>
    /// Needs to SetPublisherPath for DefaultRazorProjectChangePublisher
    /// </summary>
    internal abstract class RazorProjectHostBase : OnceInitializedOnceDisposedAsync, IProjectDynamicLoadComponent
    {
        private readonly Workspace _workspace;
        private readonly AsyncSemaphore _lock;

        private ProjectSnapshotManagerBase _projectManager;
        private readonly Dictionary<string, HostDocument> _currentDocuments;
        protected readonly ProjectConfigurationFilePathStore _projectConfigurationFilePathStore;

        internal const string BaseIntermediateOutputPathPropertyName = "BaseIntermediateOutputPath";
        internal const string IntermediateOutputPathPropertyName = "IntermediateOutputPath";
        internal const string MSBuildProjectDirectoryPropertyName = "MSBuildProjectDirectory";

        internal const string ConfigurationGeneralSchemaName = "ConfigurationGeneral";

        // Internal settable for testing
        // 250ms between publishes to prevent bursts of changes yet still be responsive to changes.
        internal int EnqueueDelay { get; set; } = 250;

        public RazorProjectHostBase(
            IUnconfiguredProjectCommonServices commonServices,
            [Import(typeof(VisualStudioWorkspace))] Workspace workspace,
            ProjectConfigurationFilePathStore projectConfigurationFilePathStore)
            : base(commonServices.ThreadingService.JoinableTaskContext)
        {
            if (commonServices == null)
            {
                throw new ArgumentNullException(nameof(commonServices));
            }

            if (workspace == null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            CommonServices = commonServices;
            _workspace = workspace;

            _lock = new AsyncSemaphore(initialCount: 1);
            _currentDocuments = new Dictionary<string, HostDocument>(FilePathComparer.Instance);
            _projectConfigurationFilePathStore = projectConfigurationFilePathStore;
        }

        // Internal for testing
        protected RazorProjectHostBase(
            IUnconfiguredProjectCommonServices commonServices,
            Workspace workspace,
            ProjectConfigurationFilePathStore projectConfigurationFilePathStore,
            ProjectSnapshotManagerBase projectManager)
            : this(commonServices, workspace, projectConfigurationFilePathStore)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
        }

        protected HostProject Current { get; private set; }

        protected IUnconfiguredProjectCommonServices CommonServices { get; }

        // internal for tests. The product will call through the IProjectDynamicLoadComponent interface.
        internal Task LoadAsync()
        {
            return InitializeAsync();
        }

        protected override Task InitializeCoreAsync(CancellationToken cancellationToken)
        {
            CommonServices.UnconfiguredProject.ProjectRenaming += UnconfiguredProject_ProjectRenaming;

            return Task.CompletedTask;
        }

        protected override async Task DisposeCoreAsync(bool initialized)
        {
            if (initialized)
            {
                CommonServices.UnconfiguredProject.ProjectRenaming -= UnconfiguredProject_ProjectRenaming;

                await ExecuteWithLock(async () =>
                {
                    if (Current != null)
                    {
                        await UpdateAsync(UninitializeProjectUnsafe).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }

        // Internal for tests
        internal async Task OnProjectRenamingAsync()
        {
            // When a project gets renamed we expect any rules watched by the derived class to fire.
            //
            // However, the project snapshot manager uses the project Fullpath as the key. We want to just
            // reinitialize the HostProject with the same configuration and settings here, but the updated
            // FilePath.
            await ExecuteWithLock(async () =>
            {
                if (Current != null)
                {
                    var old = Current;
                    var oldDocuments = _currentDocuments.Values.ToArray();

                    await UpdateAsync(UninitializeProjectUnsafe).ConfigureAwait(false);

                    await UpdateAsync(() =>
                    {
                        var filePath = CommonServices.UnconfiguredProject.FullPath;
                        UpdateProjectUnsafe(new HostProject(filePath, old.Configuration, old.RootNamespace));

                        // This should no-op in the common case, just putting it here for insurance.
                        for (var i = 0; i < oldDocuments.Length; i++)
                        {
                            AddDocumentUnsafe(oldDocuments[i]);
                        }
                    }).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        // Should only be called from the UI thread.
        private ProjectSnapshotManagerBase GetProjectManager()
        {
            CommonServices.ThreadingService.VerifyOnUIThread();

            if (_projectManager == null)
            {
                _projectManager = (ProjectSnapshotManagerBase)_workspace.Services.GetLanguageServices(RazorLanguage.Name).GetRequiredService<ProjectSnapshotManager>();
            }

            return _projectManager;
        }

        protected async Task UpdateAsync(Action action)
        {
            await CommonServices.ThreadingService.SwitchToUIThread();
            action();
        }

        protected void UninitializeProjectUnsafe()
        {
            ClearDocumentsUnsafe();
            UpdateProjectUnsafe(null);
        }

        protected void UpdateProjectUnsafe(HostProject project)
        {
            var projectManager = GetProjectManager();
            if (Current == null && project == null)
            {
                // This is a no-op. This project isn't using Razor.
            }
            else if (Current == null && project != null)
            {
                projectManager.ProjectAdded(project);
            }
            else if (Current != null && project == null)
            {
                Debug.Assert(_currentDocuments.Count == 0);
                projectManager.ProjectRemoved(Current);
                _projectConfigurationFilePathStore.Remove(Current.FilePath);
            }
            else
            {
                projectManager.ProjectConfigurationChanged(project);
            }

            Current = project;
        }

        protected void AddDocumentUnsafe(HostDocument document)
        {
            var projectManager = GetProjectManager();

            if (_currentDocuments.ContainsKey(document.FilePath))
            {
                // Ignore duplicates
                return;
            }

            projectManager.DocumentAdded(Current, document, new FileTextLoader(document.FilePath, null));
            _currentDocuments.Add(document.FilePath, document);
        }

        protected void RemoveDocumentUnsafe(HostDocument document)
        {
            var projectManager = GetProjectManager();

            projectManager.DocumentRemoved(Current, document);
            _currentDocuments.Remove(document.FilePath);
        }

        protected void ClearDocumentsUnsafe()
        {
            var projectManager = GetProjectManager();

            foreach (var kvp in _currentDocuments)
            {
                projectManager.DocumentRemoved(Current, kvp.Value);
            }

            _currentDocuments.Clear();
        }
        
        protected async Task ExecuteWithLock(Func<Task> func)
        {
            using (JoinableCollection.Join())
            {
                using (await _lock.EnterAsync().ConfigureAwait(false))
                {
                    var task = JoinableFactory.RunAsync(func);
                    await task.Task.ConfigureAwait(false);
                }
            }
        }

        Task IProjectDynamicLoadComponent.LoadAsync()
        {
            return InitializeAsync();
        }

        Task IProjectDynamicLoadComponent.UnloadAsync()
        {
            return DisposeAsync();
        }

        private async Task UnconfiguredProject_ProjectRenaming(object sender, ProjectRenamedEventArgs args)
        {
            await OnProjectRenamingAsync().ConfigureAwait(false);
        }

        // Internal for testing
        internal static bool TryGetIntermediateOutputPath(
            IImmutableDictionary<string, IProjectRuleSnapshot> state,
            out string path)
        {
            if (!state.TryGetValue(ConfigurationGeneralSchemaName, out var rule))
            {
                path = null;
                return false;
            }

            if (!rule.Properties.TryGetValue(BaseIntermediateOutputPathPropertyName, out var baseIntermediateOutputPathValue))
            {
                path = null;
                return false;
            }

            if (!rule.Properties.TryGetValue(IntermediateOutputPathPropertyName, out var intermediateOutputPathValue))
            {
                path = null;
                return false;
            }

            if (string.IsNullOrEmpty(intermediateOutputPathValue) || string.IsNullOrEmpty(baseIntermediateOutputPathValue))
            {
                path = null;
                return false;
            }
            var basePath = new DirectoryInfo(baseIntermediateOutputPathValue).Parent;
            var joinedPath = Path.Combine(basePath.FullName, intermediateOutputPathValue);

            path = joinedPath;
            return true;
        }
    }
}
