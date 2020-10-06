// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;
using Shared = System.Composition.SharedAttribute;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// Publishes project.razor.json files.
    /// </summary>
    [Shared]
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    internal class DefaultRazorProjectChangePublisher : ProjectSnapshotChangeTrigger
    {
        internal readonly Dictionary<string, Task> _deferredPublishTasks;

        // Internal for testing
        internal bool _active;

        private const string TempFileExt = ".temp";
        private readonly RazorLogger _logger;
        private readonly LSPEditorFeatureDetector _lspEditorFeatureDetector;
        private readonly IVsOperationProgressStatusService _operationProgressStatusService = null;
        private readonly ProjectConfigurationFilePathStore _projectConfigurationFilePathStore;
        private readonly Dictionary<string, ProjectSnapshot> _pendingProjectPublishes;
        private readonly object _publishLock;

        private readonly JsonSerializer _serializer = new JsonSerializer();
        private ProjectSnapshotManagerBase _projectSnapshotManager;

        [ImportingConstructor]
        public DefaultRazorProjectChangePublisher(
            LSPEditorFeatureDetector lSPEditorFeatureDetector,
            ProjectConfigurationFilePathStore projectConfigurationFilePathStore,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            RazorLogger logger)
        {
            if (lSPEditorFeatureDetector is null)
            {
                throw new ArgumentNullException(nameof(lSPEditorFeatureDetector));
            }

            if (projectConfigurationFilePathStore is null)
            {
                throw new ArgumentNullException(nameof(projectConfigurationFilePathStore));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (serviceProvider is null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _deferredPublishTasks = new Dictionary<string, Task>(FilePathComparer.Instance);
            _pendingProjectPublishes = new Dictionary<string, ProjectSnapshot>(FilePathComparer.Instance);
            _publishLock = new object();

            _lspEditorFeatureDetector = lSPEditorFeatureDetector;
            _projectConfigurationFilePathStore = projectConfigurationFilePathStore;
            _logger = logger;

            _serializer.Converters.Add(TagHelperDescriptorJsonConverter.Instance);
            _serializer.Converters.Add(RazorConfigurationJsonConverter.Instance);
            _serializer.Converters.Add(CodeAnalysis.Razor.Workspaces.Serialization.ProjectSnapshotJsonConverter.Instance);

            if (serviceProvider.GetService(typeof(SVsOperationProgress)) is IVsOperationProgressStatusService service)
            {
                _operationProgressStatusService = service;
            }
        }

        // Internal settable for testing
        // 3000ms between publishes to prevent bursts of changes yet still be responsive to changes.
        internal int EnqueueDelay { get; set; } = 3000;

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectSnapshotManager = projectManager;
            _projectSnapshotManager.Changed += ProjectSnapshotManager_Changed;
        }

        // Internal for testing
        internal void EnqueuePublish(ProjectSnapshot projectSnapshot)
        {
            // A race is not possible here because we use the main thread to synchronize the updates
            // by capturing the sync context.
            _pendingProjectPublishes[projectSnapshot.FilePath] = projectSnapshot;

            if (!_deferredPublishTasks.TryGetValue(projectSnapshot.FilePath, out var update) || update.IsCompleted)
            {
                _deferredPublishTasks[projectSnapshot.FilePath] = PublishAfterDelayAsync(projectSnapshot.FilePath);
            }
        }

        internal void ProjectSnapshotManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            if (!_lspEditorFeatureDetector.IsLSPEditorAvailable(args.ProjectFilePath, hierarchy: null))
            {
                return;
            }

            // Prior to doing any sort of project state serialization/publishing we avoid any excess work as long as there aren't any "open" Razor files.
            // However, once a Razor file is opened we turn the firehose on and start doing work.
            // By taking this lazy approach we ensure that we don't do any excess Razor work (serialization is expensive) in non-Razor scenarios.

            if (!_active)
            {
                // Not currently active, we need to decide if we should become active or if we should no-op.

                if (_projectSnapshotManager.OpenDocuments.Count > 0)
                {
                    // A Razor document was just opened, we should become "active" which means we'll constantly be monitoring project state.
                    _active = true;

                    if (args.Newer?.ProjectWorkspaceState != null)
                    {
                        // Typically document open events don't result in us re-processing project state; however, given this is the first time a user opened a Razor document we should.
                        // Don't enqueue, just publish to get the most immediate result.
                        Publish(args.Newer);
                        return;
                    }
                }
                else
                {
                    // No open documents and not active. No-op.
                    return;
                }
            }

            // All the below Publish's (except ProjectRemoved) wait until our project has been initialized (ProjectWorkspaceState != null)
            // so that we don't publish half-finished projects, which can cause things like Semantic coloring to "flash"
            // when they update repeatedly as they load.
            switch (args.Kind)
            {
                case ProjectChangeKind.DocumentRemoved:
                case ProjectChangeKind.DocumentAdded:
                case ProjectChangeKind.ProjectChanged:

                    if (args.Newer.ProjectWorkspaceState != null)
                    {
                        // These changes can come in bursts so we don't want to overload the publishing system. Therefore,
                        // we enqueue publishes and then publish the latest project after a delay.
                        EnqueuePublish(args.Newer);
                    }
                    break;

                case ProjectChangeKind.ProjectAdded:

                    if (args.Newer.ProjectWorkspaceState != null)
                    {
                        Publish(args.Newer);
                    }
                    break;

                case ProjectChangeKind.ProjectRemoved:
                    RemovePublishingData(args.Older);
                    break;
            }
        }

        // Internal for testing
        internal void Publish(ProjectSnapshot projectSnapshot)
        {
            if (projectSnapshot is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshot));
            }

            lock (_publishLock)
            {
                string configurationFilePath = null;
                try
                {
                    if (!_projectConfigurationFilePathStore.TryGet(projectSnapshot.FilePath, out configurationFilePath))
                    {
                        return;
                    }


                    // We don't want to serialize the project until it's ready to avoid flashing as the project loads different parts.
                    // Since the project.razor.json from last session likely still exists the experience is unlikely to be degraded by this delay.
                    // An exception is made for when there's no existing project.razor.json because some flashing is preferable to having no TagHelper knowledge.
                    if (ShouldSerialize(configurationFilePath))
                    {
                        SerializeToFile(projectSnapshot, configurationFilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($@"Could not update Razor project configuration file '{configurationFilePath}':
{ex}");
                }
            }
        }

        // Internal for testing
        internal void RemovePublishingData(ProjectSnapshot projectSnapshot)
        {
            lock (_publishLock)
            {
                var oldProjectFilePath = projectSnapshot.FilePath;
                if (!_projectConfigurationFilePathStore.TryGet(oldProjectFilePath, out var configurationFilePath))
                {
                    // If we don't track the value in PublishFilePathMappings that means it's already been removed, do nothing.
                    return;
                }

                if (_pendingProjectPublishes.TryGetValue(oldProjectFilePath, out _))
                {
                    // Project was removed while a delayed publish was in flight. Clear the in-flight publish so it noops.
                    _pendingProjectPublishes.Remove(oldProjectFilePath);
                }
            }
        }

        protected virtual void SerializeToFile(ProjectSnapshot projectSnapshot, string publishFilePath)
        {
            // We need to avoid having an incomplete file at any point, but our
            // project.razor.json is large enough that it will be written as multiple operations.
            var tempFilePath = string.Concat(publishFilePath, TempFileExt);
            var tempFileInfo = new FileInfo(tempFilePath);

            if (tempFileInfo.Exists)
            {
                // This could be caused by failures during serialization or early process termination.
                tempFileInfo.Delete();
            }

            // This needs to be in explicit brackets because the operation needs to be completed
            // by the time we move the tempfile into its place
            using (var writer = tempFileInfo.CreateText())
            {
                _serializer.Serialize(writer, projectSnapshot);

                var fileInfo = new FileInfo(publishFilePath);
                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }
            }

            tempFileInfo.MoveTo(publishFilePath);
        }

        protected virtual bool ShouldSerialize(string configurationFilePath)
        {
            if (!File.Exists(configurationFilePath))
            {
                return true;
            }

            var status = _operationProgressStatusService?.GetStageStatusForSolutionLoad(CommonOperationProgressStageIds.Intellisense);

            if (status is null)
            {
                return true;
            }

            return !status.IsInProgress;
        }

        private async Task PublishAfterDelayAsync(string projectFilePath)
        {
            await Task.Delay(EnqueueDelay).ConfigureAwait(false);

            if (!_pendingProjectPublishes.TryGetValue(projectFilePath, out var projectSnapshot))
            {
                // Project was removed while waiting for the publish delay.
                return;
            }

            _pendingProjectPublishes.Remove(projectFilePath);

            Publish(projectSnapshot);
        }
    }
}
