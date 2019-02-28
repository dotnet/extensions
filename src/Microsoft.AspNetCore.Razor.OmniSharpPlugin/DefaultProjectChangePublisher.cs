// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
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
        private readonly ILogger<DefaultProjectChangePublisher> _logger;
        private readonly JsonSerializer _serializer;
        private readonly Dictionary<string, string> _publishFilePathMappings;
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
            _publishLock = new object();
        }

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
            using (var writer = fileInfo.CreateText())
            {
                _serializer.Serialize(writer, projectSnapshot);
            }
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

        private void ProjectManager_Changed(object sender, OmniSharpProjectChangeEventArgs args)
        {
            switch (args.Kind)
            {
                case OmniSharpProjectChangeKind.ProjectChanged:
                case OmniSharpProjectChangeKind.ProjectAdded:
                    Publish(args.Newer);
                    break;
                case OmniSharpProjectChangeKind.ProjectRemoved:
                    lock (_publishLock)
                    {
                        if (_publishFilePathMappings.TryGetValue(args.Older.FilePath, out var publishFilePath))
                        {
                            DeleteFile(publishFilePath);
                        }
                    }
                    break;
            }
        }
    }
}
