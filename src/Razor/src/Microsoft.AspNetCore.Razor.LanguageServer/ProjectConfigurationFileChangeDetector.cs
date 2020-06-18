// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class ProjectConfigurationFileChangeDetector : IFileChangeDetector
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IEnumerable<IProjectConfigurationFileChangeListener> _listeners;
        private FileSystemWatcher _watcher;

        public ProjectConfigurationFileChangeDetector(
            ForegroundDispatcher foregroundDispatcher,
            FilePathNormalizer filePathNormalizer,
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

            if (listeners is null)
            {
                throw new ArgumentNullException(nameof(listeners));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _filePathNormalizer = filePathNormalizer;
            _listeners = listeners;
        }

        public async Task StartAsync(string workspaceDirectory, CancellationToken cancellationToken)
        {
            if (workspaceDirectory is null)
            {
                throw new ArgumentNullException(nameof(workspaceDirectory));
            }

            // Dive through existing project configuration files and fabricate "added" events so listeners can accurately listen to state changes for them.

            workspaceDirectory = _filePathNormalizer.Normalize(workspaceDirectory);
            var existingConfigurationFiles = GetExistingConfigurationFiles(workspaceDirectory);

            await Task.Factory.StartNew(() =>
            {
                foreach (var configurationFilePath in existingConfigurationFiles)
                {
                    FileSystemWatcher_ProjectConfigurationFileEvent(configurationFilePath, RazorFileChangeKind.Added);
                }
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            // This is an entry point for testing
            OnInitializationFinished();

            if (cancellationToken.IsCancellationRequested)
            {
                // Client cancelled connection, no need to setup any file watchers. Server is about to tear down.
                return;
            }

            _watcher = new RazorFileSystemWatcher(workspaceDirectory, LanguageServerConstants.ProjectConfigurationFile)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
            };

            _watcher.Created += (sender, args) => FileSystemWatcher_ProjectConfigurationFileEvent_Background(args.FullPath, RazorFileChangeKind.Added);
            _watcher.Deleted += (sender, args) => FileSystemWatcher_ProjectConfigurationFileEvent_Background(args.FullPath, RazorFileChangeKind.Removed);
            _watcher.Changed += (sender, args) => FileSystemWatcher_ProjectConfigurationFileEvent_Background(args.FullPath, RazorFileChangeKind.Changed);
            _watcher.Renamed += (sender, args) =>
            {
                // Translate file renames into remove / add

                if (args.OldFullPath.EndsWith(LanguageServerConstants.ProjectConfigurationFile, FilePathComparison.Instance))
                {
                    // Renaming from project.razor.json to something else. Just remove the configuration file.
                    FileSystemWatcher_ProjectConfigurationFileEvent_Background(args.OldFullPath, RazorFileChangeKind.Removed);
                }
                else if (args.FullPath.EndsWith(LanguageServerConstants.ProjectConfigurationFile, FilePathComparison.Instance))
                {
                    // Renaming from a non-project.razor.json file to project.razor.json. Just add the configuration file.
                    FileSystemWatcher_ProjectConfigurationFileEvent_Background(args.FullPath, RazorFileChangeKind.Added);
                }
            };

            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            // We're relying on callers to synchronize start/stops so we don't need to ensure one happens before the other.

            _watcher?.Dispose();
            _watcher = null;
        }

        // Protected virtual for testing
        protected virtual void OnInitializationFinished()
        {
        }

        // Protected virtual for testing
        protected virtual IReadOnlyList<string> GetExistingConfigurationFiles(string workspaceDirectory)
        {
            return Directory.GetFiles(workspaceDirectory, LanguageServerConstants.ProjectConfigurationFile, SearchOption.AllDirectories);
        }

        private void FileSystemWatcher_ProjectConfigurationFileEvent_Background(string physicalFilePath, RazorFileChangeKind kind)
        {
            Task.Factory.StartNew(
                () => FileSystemWatcher_ProjectConfigurationFileEvent(physicalFilePath, kind),
                CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
        }

        private void FileSystemWatcher_ProjectConfigurationFileEvent(string physicalFilePath, RazorFileChangeKind kind)
        {
            var args = new ProjectConfigurationFileChangeEventArgs(physicalFilePath, kind);
            foreach (var listener in _listeners)
            {
                listener.ProjectConfigurationFileChanged(args);
            }
        }
    }
}
