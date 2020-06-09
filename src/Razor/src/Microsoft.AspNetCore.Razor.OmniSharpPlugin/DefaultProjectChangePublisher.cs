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

        // Virtual for testing
        protected virtual void SerializeToFile(OmniSharpProjectSnapshot projectSnapshot, string publishFilePath)
        {
            var fileInfo = new FileInfo(publishFilePath);
            using var writer = fileInfo.CreateText();
            _serializer.Serialize(writer, projectSnapshot);
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
            // A race is not possible here because we use the main thread to synchronize the updates
            // by capturing the sync context.

            _pendingProjectPublishes[projectSnapshot.FilePath] = projectSnapshot;

            if (!_deferredPublishTasks.TryGetValue(projectSnapshot.FilePath, out var update) || update.IsCompleted)
            {
                _deferredPublishTasks[projectSnapshot.FilePath] = PublishAfterDelay(projectSnapshot.FilePath);
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

                    EnqueuePublish(args.Newer);
                    break;
                case OmniSharpProjectChangeKind.ProjectAdded:
                    Publish(args.Newer);
                    break;
                case OmniSharpProjectChangeKind.ProjectRemoved:
                    lock (_publishLock)
                    {
                        var oldProjectFilePath = args.Older.FilePath;
                        if (_publishFilePathMappings.TryGetValue(oldProjectFilePath, out var publishFilePath))
                        {
                            if (_pendingProjectPublishes.TryGetValue(oldProjectFilePath, out _))
                            {
                                // Project was removed while a delayed publish was in flight. Clear the in-flight publish so it noops.
                                _pendingProjectPublishes.Remove(oldProjectFilePath);
                            }

                            DeleteFile(publishFilePath);
                        }
                    }
                    break;
            }
        }

        private async Task PublishAfterDelay(string projectFilePath)
        {
            await Task.Delay(EnqueueDelay);

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
