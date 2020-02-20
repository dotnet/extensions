// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorFileChangeDetector : IFileChangeDetector
    {
        private static readonly IReadOnlyList<string> RazorFileExtensions = new[] { ".razor", ".cshtml" };
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IEnumerable<IRazorFileChangeListener> _listeners;
        private readonly List<FileSystemWatcher> _watchers;

        public RazorFileChangeDetector(
            ForegroundDispatcher foregroundDispatcher,
            FilePathNormalizer filePathNormalizer,
            IEnumerable<IRazorFileChangeListener> listeners)
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
            _watchers = new List<FileSystemWatcher>(RazorFileExtensions.Count);
        }

        public async Task StartAsync(string workspaceDirectory, CancellationToken cancellationToken)
        {
            if (workspaceDirectory is null)
            {
                throw new ArgumentNullException(nameof(workspaceDirectory));
            }

            // Dive through existing Razor files and fabricate "added" events so listeners can accurately listen to state changes for them.

            workspaceDirectory = _filePathNormalizer.NormalizeForRead(workspaceDirectory);
            var existingRazorFiles = GetExistingRazorFiles(workspaceDirectory);

            await Task.Factory.StartNew(() =>
            {
                foreach (var razorFilePath in existingRazorFiles)
                {
                    FileSystemWatcher_RazorFileEvent(razorFilePath, RazorFileChangeKind.Added);
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

            for (var i = 0; i < RazorFileExtensions.Count; i++)
            {
                var extension = RazorFileExtensions[i];
                var watcher = new FileSystemWatcher(workspaceDirectory, "*" + extension)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    IncludeSubdirectories = true,
                };

                watcher.Created += (sender, args) => FileSystemWatcher_RazorFileEvent_Background(args.FullPath, RazorFileChangeKind.Added);
                watcher.Deleted += (sender, args) => FileSystemWatcher_RazorFileEvent_Background(args.FullPath, RazorFileChangeKind.Removed);
                watcher.Renamed += (sender, args) =>
                {
                    // Translate file renames into remove->add

                    if (args.OldFullPath.EndsWith(extension, FilePathComparison.Instance))
                    {
                        // Renaming from Razor file to something else.
                        FileSystemWatcher_RazorFileEvent_Background(args.OldFullPath, RazorFileChangeKind.Removed);
                    }

                    if (args.FullPath.EndsWith(extension, FilePathComparison.Instance))
                    {
                        // Renaming to a Razor file.
                        FileSystemWatcher_RazorFileEvent_Background(args.FullPath, RazorFileChangeKind.Added);
                    }
                };

                watcher.EnableRaisingEvents = true;

                _watchers.Add(watcher);
            }
        }

        // Protected virtual for testing
        protected virtual void OnInitializationFinished()
        {
        }

        // Protected virtual for testing
        protected virtual IReadOnlyList<string> GetExistingRazorFiles(string workspaceDirectory)
        {
            var existingRazorFiles = Enumerable.Empty<string>();
            for (var i = 0; i < RazorFileExtensions.Count; i++)
            {
                var extension = RazorFileExtensions[i];
                var existingFiles = Directory.GetFiles(workspaceDirectory, "*" + extension, SearchOption.AllDirectories);
                existingRazorFiles = existingRazorFiles.Concat(existingFiles);
            }

            return existingRazorFiles.ToArray();
        }

        private void FileSystemWatcher_RazorFileEvent_Background(string physicalFilePath, RazorFileChangeKind kind)
        {
            Task.Factory.StartNew(
                () => FileSystemWatcher_RazorFileEvent(physicalFilePath, kind),
                CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
        }

        private void FileSystemWatcher_RazorFileEvent(string physicalFilePath, RazorFileChangeKind kind)
        {
            foreach (var listener in _listeners)
            {
                listener.RazorFileChanged(physicalFilePath, kind);
            }
        }
    }
}
