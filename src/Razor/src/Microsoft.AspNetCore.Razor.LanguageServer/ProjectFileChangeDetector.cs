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
    internal class ProjectFileChangeDetector : IFileChangeDetector
    {
        private const string ProjectFileExtension = ".csproj";
        private const string ProjectFileExtensionPattern = "*" + ProjectFileExtension;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IEnumerable<IProjectFileChangeListener> _listeners;
        private FileSystemWatcher _watcher;

        public ProjectFileChangeDetector(
            ForegroundDispatcher foregroundDispatcher,
            FilePathNormalizer filePathNormalizer,
            IEnumerable<IProjectFileChangeListener> listeners)
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

            // Dive through existing project files and fabricate "added" events so listeners can accurately listen to state changes for them.

            workspaceDirectory = _filePathNormalizer.Normalize(workspaceDirectory);
            var existingProjectFiles = GetExistingProjectFiles(workspaceDirectory);

            await Task.Factory.StartNew(() =>
            {
                foreach (var projectFilePath in existingProjectFiles)
                {
                    FileSystemWatcher_ProjectFileEvent(projectFilePath, RazorFileChangeKind.Added);
                }
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);

            // This is an entry point for testing
            OnInitializationFinished();

            if (cancellationToken.IsCancellationRequested)
            {
                // Client cancelled connection, no need to setup any file watchers. Server is about to tear down.
                return;
            }

            // Start listening for project file changes (added/removed/renamed).

            _watcher = new RazorFileSystemWatcher(workspaceDirectory, ProjectFileExtensionPattern)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
            };

            _watcher.Created += (sender, args) => FileSystemWatcher_ProjectFileEvent_Background(args.FullPath, RazorFileChangeKind.Added);
            _watcher.Deleted += (sender, args) => FileSystemWatcher_ProjectFileEvent_Background(args.FullPath, RazorFileChangeKind.Removed);
            _watcher.Renamed += (sender, args) =>
            {
                // Translate file renames into remove->add

                if (args.OldFullPath.EndsWith(ProjectFileExtension, FilePathComparison.Instance))
                {
                    // Renaming from project file to something else.
                    FileSystemWatcher_ProjectFileEvent_Background(args.OldFullPath, RazorFileChangeKind.Removed);
                }

                if (args.FullPath.EndsWith(ProjectFileExtension, FilePathComparison.Instance))
                {
                    // Renaming to a project file.
                    FileSystemWatcher_ProjectFileEvent_Background(args.FullPath, RazorFileChangeKind.Added);
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
        protected virtual IReadOnlyList<string> GetExistingProjectFiles(string workspaceDirectory)
        {
            return Directory.GetFiles(workspaceDirectory, ProjectFileExtensionPattern, SearchOption.AllDirectories);
        }

        private void FileSystemWatcher_ProjectFileEvent_Background(string physicalFilePath, RazorFileChangeKind kind)
        {
            Task.Factory.StartNew(
                () => FileSystemWatcher_ProjectFileEvent(physicalFilePath, kind),
                CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
        }

        private void FileSystemWatcher_ProjectFileEvent(string physicalFilePath, RazorFileChangeKind kind)
        {
            foreach (var listener in _listeners)
            {
                listener.ProjectFileChanged(physicalFilePath, kind);
            }
        }
    }
}
