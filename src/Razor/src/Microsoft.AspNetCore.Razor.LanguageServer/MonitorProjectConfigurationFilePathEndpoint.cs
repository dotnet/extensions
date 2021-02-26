// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class MonitorProjectConfigurationFilePathEndpoint : IMonitorProjectConfigurationFilePathHandler, IDisposable
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly WorkspaceDirectoryPathResolver _workspaceDirectoryPathResolver;
        private readonly IEnumerable<IProjectConfigurationFileChangeListener> _listeners;
        private readonly ConcurrentDictionary<string, (string ConfigurationDirectory, IFileChangeDetector Detector)> _outputPathMonitors;
        private readonly object _disposeLock;
        private bool _disposed;

        public MonitorProjectConfigurationFilePathEndpoint(
            ForegroundDispatcher foregroundDispatcher,
            FilePathNormalizer filePathNormalizer,
            WorkspaceDirectoryPathResolver workspaceDirectoryPathResolver,
            IEnumerable<IProjectConfigurationFileChangeListener> listeners)
        {
            if (foregroundDispatcher is null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (workspaceDirectoryPathResolver is null)
            {
                throw new ArgumentNullException(nameof(workspaceDirectoryPathResolver));
            }

            if (listeners is null)
            {
                throw new ArgumentNullException(nameof(listeners));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _filePathNormalizer = filePathNormalizer;
            _workspaceDirectoryPathResolver = workspaceDirectoryPathResolver;
            _listeners = listeners;
            _outputPathMonitors = new ConcurrentDictionary<string, (string, IFileChangeDetector)>(FilePathComparer.Instance);
            _disposeLock = new object();
        }

        public async Task<Unit> Handle(MonitorProjectConfigurationFilePathParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return Unit.Value;
                }
            }

            if (request.ConfigurationFilePath == null)
            {
                RemoveMonitor(request.ProjectFilePath);

                return Unit.Value;
            }

            if (!request.ConfigurationFilePath.EndsWith(LanguageServerConstants.ProjectConfigurationFile, StringComparison.Ordinal))
            {
                Debug.Fail("We should only ever be given configuration file paths with a project.razor.json suffix.");
                return Unit.Value;
            }

            var configurationDirectory = Path.GetDirectoryName(request.ConfigurationFilePath);
            var normalizedConfigurationDirectory = _filePathNormalizer.NormalizeDirectory(configurationDirectory);
            var workspaceDirectory = _workspaceDirectoryPathResolver.Resolve();
            var normalizedWorkspaceDirectory = _filePathNormalizer.NormalizeDirectory(workspaceDirectory);

            var previousMonitorExists = _outputPathMonitors.TryGetValue(request.ProjectFilePath, out var entry);

            if (normalizedConfigurationDirectory.StartsWith(normalizedWorkspaceDirectory, FilePathComparison.Instance))
            {
                if (previousMonitorExists)
                {
                    // Configuration directory changed from an external directory -> internal directory.
                    RemoveMonitor(request.ProjectFilePath);
                }

                // Configuration directory is already in the workspace directory. We already monitor everything in the workspace directory.
                return Unit.Value;
            }

            if (previousMonitorExists)
            {
                if (FilePathComparer.Instance.Equals(configurationDirectory, entry.ConfigurationDirectory))
                {
                    // Already tracking the requested configuration output path for this project
                    return Unit.Value;
                }

                // Projects configuration output path has changed. Stop existing detector so we can restart it with a new directory.
                entry.Detector.Stop();
            }
            else
            {
                var detector = CreateFileChangeDetector();
                entry = (configurationDirectory, detector);

                if (!_outputPathMonitors.TryAdd(request.ProjectFilePath, entry))
                {
                    // There's a concurrent request going on for this specific project. To avoid calling "StartAsync" twice we return early.
                    // Note: This is an extremely edge case race condition that should in practice never happen due to how long it takes to calculate project state changes
                    return Unit.Value;
                }
            }

            await entry.Detector.StartAsync(configurationDirectory, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                // Request was cancelled while starting the detector. Need to stop it so we don't leak.
                entry.Detector.Stop();
                return Unit.Value;
            }

            if (!_outputPathMonitors.ContainsKey(request.ProjectFilePath))
            {
                // This can happen if there were multiple concurrent requests to "remove" and "update" file change detectors for the same project path.
                // In that case we need to stop the detector to ensure we don't leak.
                entry.Detector.Stop();
                return Unit.Value;
            }

            lock (_disposeLock)
            {
                if (_disposed)
                {
                    // Server's being stopped.
                    entry.Detector.Stop();
                }
            }

            return Unit.Value;
        }

        private void RemoveMonitor(string projectFilePath)
        {
            // Should no longer monitor configuration output paths for the project
            if (_outputPathMonitors.TryRemove(projectFilePath, out var removedEntry))
            {
                removedEntry.Detector.Stop();
            }
            else
            {
                // Concurrent requests to remove the same configuration output path for the project.  We've already
                // done the removal so we can just return gracefully.
            }
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            foreach (var entry in _outputPathMonitors)
            {
                entry.Value.Detector.Stop();
            }
        }

        // Protected virtual for testing
        protected virtual IFileChangeDetector CreateFileChangeDetector() => new ProjectConfigurationFileChangeDetector(_foregroundDispatcher, _filePathNormalizer, _listeners);
    }
}
