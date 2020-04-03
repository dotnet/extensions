// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    /// <summary>
    /// Publishes project.razor.json files.
    /// </summary>
    [Shared]
    [Export(typeof(ProjectSnapshotChangeTrigger))]
    [Export(typeof(RazorProjectChangePublisher))]
    internal class DefaultRazorProjectChangePublisher : RazorProjectChangePublisher
    {
        internal readonly Dictionary<string, Task> _deferredPublishTasks;
        private readonly JoinableTaskContext _joinableTaskContext;
        private readonly RazorLogger _logger;
        private readonly LSPEditorFeatureDetector _lspEditorFeatureDetector;
        private readonly Dictionary<string, ProjectSnapshot> _pendingProjectPublishes;
        private readonly object _publishLock;

        private readonly JsonSerializer _serializer = new JsonSerializer()
        {
            Formatting = Formatting.Indented
        };

        private ProjectSnapshotManagerBase _projectSnapshotManager;

        [ImportingConstructor]
        public DefaultRazorProjectChangePublisher(
                    JoinableTaskContext joinableTaskContext,
                    LSPEditorFeatureDetector lSPEditorFeatureDetector,
                    RazorLogger logger)
        {
            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            if (lSPEditorFeatureDetector is null)
            {
                throw new ArgumentNullException(nameof(lSPEditorFeatureDetector));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _deferredPublishTasks = new Dictionary<string, Task>(FilePathComparer.Instance);
            _pendingProjectPublishes = new Dictionary<string, ProjectSnapshot>(FilePathComparer.Instance);
            _publishLock = new object();

            _lspEditorFeatureDetector = lSPEditorFeatureDetector;

            _logger = logger;

            _serializer.Converters.Add(TagHelperDescriptorJsonConverter.Instance);
            _serializer.Converters.Add(RazorConfigurationJsonConverter.Instance);
            _serializer.Converters.Add(CodeAnalysis.Razor.Workspaces.Serialization.ProjectSnapshotJsonConverter.Instance);
            _joinableTaskContext = joinableTaskContext;
        }

        // Internal settable for testing
        // 250ms between publishes to prevent bursts of changes yet still be responsive to changes.
        internal int EnqueueDelay { get; set; } = 250;

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectSnapshotManager = projectManager;
            _projectSnapshotManager.Changed += ProjectSnapshotManager_Changed;
        }

        public override void RemovePublishFilePath(string projectFilePath)
        {
            Debug.Assert(_joinableTaskContext.IsOnMainThread, "RemovePublishFilePath should have been on main thread");
            PublishFilePathMappings.TryRemove(projectFilePath, out var _);
        }

        public override void SetPublishFilePath(string projectFilePath, string publishFilePath)
        {
            // Should only be called from the main thread.
            Debug.Assert(_joinableTaskContext.IsOnMainThread, "SetPublishFilePath should have been on main thread");
            PublishFilePathMappings[projectFilePath] = publishFilePath;
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

            switch (args.Kind)
            {
                case ProjectChangeKind.DocumentRemoved:
                case ProjectChangeKind.DocumentAdded:
                case ProjectChangeKind.DocumentChanged:
                case ProjectChangeKind.ProjectChanged:
                    // These changes can come in bursts so we don't want to overload the publishing system. Therefore,
                    // we enqueue publishes and then publish the latest project after a delay.

                    EnqueuePublish(args.Newer);
                    break;

                case ProjectChangeKind.ProjectAdded:
                    Publish(args.Newer);
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
                string publishFilePath = null;
                try
                {
                    if (!PublishFilePathMappings.TryGetValue(projectSnapshot.FilePath, out publishFilePath))
                    {
                        return;
                    }

                    SerializeToFile(projectSnapshot, publishFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($@"Could not update Razor project configuration file '{publishFilePath}':
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
                if (!PublishFilePathMappings.TryGetValue(oldProjectFilePath, out var publishFilePath))
                {
                    // If we don't track the value in PublishFilePathMappings that means it's already been removed, do nothing.
                    return;
                }

                if (_pendingProjectPublishes.TryGetValue(oldProjectFilePath, out _))
                {
                    // Project was removed while a delayed publish was in flight. Clear the in-flight publish so it noops.
                    _pendingProjectPublishes.Remove(oldProjectFilePath);
                }

                DeleteFile(publishFilePath);
            }
        }

        // Virtual for testing
        protected virtual void DeleteFile(string publishFilePath)
        {
            var info = new FileInfo(publishFilePath);
            if (info.Exists)
            {
                try
                {
                    // Try catch around the delete in case it was deleted between the Exists and this delete call. This also
                    // protects against unauthorized access issues.
                    info.Delete();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($@"Failed to delete Razor configuration file '{publishFilePath}':
{ex}");
                }
            }
        }

        protected virtual void SerializeToFile(ProjectSnapshot projectSnapshot, string publishFilePath)
        {
            var fileInfo = new FileInfo(publishFilePath);
            using var writer = fileInfo.CreateText();
            _serializer.Serialize(writer, projectSnapshot);
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