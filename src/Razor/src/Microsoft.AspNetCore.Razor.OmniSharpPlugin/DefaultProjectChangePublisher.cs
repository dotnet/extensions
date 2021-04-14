// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(ProjectChangePublisher))]
    [Export(typeof(IOmniSharpProjectSnapshotManagerChangeTrigger))]
    internal class DefaultProjectChangePublisher : ProjectChangePublisher, IOmniSharpProjectSnapshotManagerChangeTrigger
    {
        private const string TempFileExt = ".temp";

        // Internal for testing
        internal readonly Dictionary<string, Task> _deferredPublishTasks;
        private readonly ILogger<DefaultProjectChangePublisher> _logger;
        private readonly JsonSerializer _serializer;
        private readonly Dictionary<string, string> _publishFilePathMappings;
        private readonly Dictionary<string, OmniSharpProjectSnapshot> _pendingProjectPublishes;
        private readonly object _publishLock;
        private OmniSharpProjectSnapshotManagerBase _projectManager;

        [ImportingConstructor]
        public DefaultProjectChangePublisher(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<DefaultProjectChangePublisher>();

            _serializer = new JsonSerializer()
            {
                Formatting = Formatting.Indented,
            };
            _serializer.Converters.RegisterOmniSharpRazorConverters();
            _publishFilePathMappings = new Dictionary<string, string>(FilePathComparer.Instance);
            _deferredPublishTasks = new Dictionary<string, Task>(FilePathComparer.Instance);
            _pendingProjectPublishes = new Dictionary<string, OmniSharpProjectSnapshot>(FilePathComparer.Instance);
            _publishLock = new object();
        }

        // Internal settable for testing
        // 250ms between publishes to prevent bursts of changes yet still be responsive to changes.
        internal int EnqueueDelay { get; set; } = 250;

        public void Initialize(OmniSharpProjectSnapshotManagerBase projectManager)
        {
            if (projectManager == null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
            _projectManager.Changed += ProjectManager_Changed;
        }

        public override void SetPublishFilePath(string projectFilePath, string publishFilePath)
        {
            lock (_publishLock)
            {
                _publishFilePathMappings[projectFilePath] = publishFilePath;
            }
        }

        // Virtual for testing
        protected virtual void SerializeToFile(OmniSharpProjectSnapshot projectSnapshot, string publishFilePath)
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
            }

            var fileInfo = new FileInfo(publishFilePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            File.Move(tempFileInfo.FullName, publishFilePath);
        }

        // Internal for testing
        internal void Publish(OmniSharpProjectSnapshot projectSnapshot)
        {
            if (projectSnapshot == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshot));
            }

            lock (_publishLock)
            {
                string publishFilePath = null;
                try
                {
                    if (!_publishFilePathMappings.TryGetValue(projectSnapshot.FilePath, out publishFilePath))
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
        internal void EnqueuePublish(OmniSharpProjectSnapshot projectSnapshot)
        {
            lock (_publishLock)
            {
                _pendingProjectPublishes[projectSnapshot.FilePath] = projectSnapshot;

                if (!_deferredPublishTasks.TryGetValue(projectSnapshot.FilePath, out var update) || update.IsCompleted)
                {
                    _deferredPublishTasks[projectSnapshot.FilePath] = PublishAfterDelayAsync(projectSnapshot.FilePath);
                }
            }
        }

        // Internal for testing
        internal void ProjectManager_Changed(object sender, OmniSharpProjectChangeEventArgs args)
        {
            switch (args.Kind)
            {
                case OmniSharpProjectChangeKind.DocumentRemoved:
                case OmniSharpProjectChangeKind.DocumentAdded:
                case OmniSharpProjectChangeKind.ProjectChanged:
                    // These changes can come in bursts so we don't want to overload the publishing system. Therefore,
                    // we enqueue publishes and then publish the latest project after a delay.

                    if (args.Newer.ProjectWorkspaceState != null)
                    {
                        EnqueuePublish(args.Newer);
                    }
                    break;
                case OmniSharpProjectChangeKind.ProjectRemoved:
                    RemovePublishingData(args.Older);
                    break;

                // We don't care about ProjectAdded scenarios because a newly added project does not have a workspace state associated with it meaning
                // it isn't interesting for us to serialize quite yet.
            }
        }

        internal void RemovePublishingData(OmniSharpProjectSnapshot projectSnapshot)
        {
            lock (_publishLock)
            {
                var oldProjectFilePath = projectSnapshot.FilePath;
                if (!_publishFilePathMappings.TryGetValue(oldProjectFilePath, out var configurationFilePath))
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

        private async Task PublishAfterDelayAsync(string projectFilePath)
        {
            await Task.Delay(EnqueueDelay);

            lock (_publishLock)
            {
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
}
