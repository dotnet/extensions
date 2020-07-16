// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Internal for testing
        internal readonly Dictionary<string, DelayedFileChangeNotification> _pendingNotifications;

        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IEnumerable<IRazorFileChangeListener> _listeners;
        private readonly List<FileSystemWatcher> _watchers;
        private readonly object _pendingNotificationsLock = new object();

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
            _pendingNotifications = new Dictionary<string, DelayedFileChangeNotification>(FilePathComparer.Instance);
        }

        // Internal for testing
        internal int EnqueueDelay { get; set; } = 1000;

        // Used in tests to ensure we can control when delayed notification work starts.
        internal ManualResetEventSlim BlockNotificationWorkStart { get; set; }

        // Used in tests to ensure we can understand when notification work noops.
        internal ManualResetEventSlim NotifyNotificationNoop { get; set; }

        public async Task StartAsync(string workspaceDirectory, CancellationToken cancellationToken)
        {
            if (workspaceDirectory is null)
            {
                throw new ArgumentNullException(nameof(workspaceDirectory));
            }

            // Dive through existing Razor files and fabricate "added" events so listeners can accurately listen to state changes for them.

            workspaceDirectory = _filePathNormalizer.Normalize(workspaceDirectory);

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
                var watcher = new RazorFileSystemWatcher(workspaceDirectory, "*" + extension)
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

        public void Stop()
        {
            // We're relying on callers to synchronize start/stops so we don't need to ensure one happens before the other.

            for (var i = 0; i < _watchers.Count; i++)
            {
                _watchers[i].Dispose();
            }

            _watchers.Clear();
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

        // Internal for testing
        internal void FileSystemWatcher_RazorFileEvent_Background(string physicalFilePath, RazorFileChangeKind kind)
        {
            lock (_pendingNotificationsLock)
            {
                if (!_pendingNotifications.TryGetValue(physicalFilePath, out var currentNotification))
                {
                    currentNotification = new DelayedFileChangeNotification();
                    _pendingNotifications[physicalFilePath] = currentNotification;
                }

                if (currentNotification.ChangeKind != null)
                {
                    // We've already has a file change event for this file. Chances are we need to normalize the result.

                    Debug.Assert(currentNotification.ChangeKind == RazorFileChangeKind.Added || currentNotification.ChangeKind == RazorFileChangeKind.Removed);

                    if (currentNotification.ChangeKind != kind)
                    {
                        // Previous was added and current is removed OR previous was removed and current is added. Either way there's no
                        // actual change to notify, null it out.
                        currentNotification.ChangeKind = null;
                    }
                    else
                    {
                        Debug.Fail($"Unexpected {kind} event because our prior tracked state was the same.");
                    }
                }
                else
                {
                    currentNotification.ChangeKind = kind;
                }

                if (currentNotification.NotifyTask == null)
                {
                    // The notify task is only ever null when it's the first time we're being notified about a change to the corresponding file.
                    currentNotification.NotifyTask = NotifyAfterDelayAsync(physicalFilePath);
                }
            }
        }

        private async Task NotifyAfterDelayAsync(string physicalFilePath)
        {
            await Task.Delay(EnqueueDelay).ConfigureAwait(false);

            OnStartingDelayedNotificationWork();

            await Task.Factory.StartNew(
                () => NotifyAfterDelay_Foreground(physicalFilePath),
                CancellationToken.None, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler);
        }

        private void NotifyAfterDelay_Foreground(string physicalFilePath)
        {
            lock (_pendingNotificationsLock)
            {
                var result = _pendingNotifications.TryGetValue(physicalFilePath, out var notification);
                Debug.Assert(result, "We should always have an associated notification after delaying an update.");

                _pendingNotifications.Remove(physicalFilePath);

                if (notification.ChangeKind == null)
                {
                    // The file to be notified has been brought back to its original state.
                    // Aka Add -> Remove is equivalent to the file never having been added.

                    OnNoopingNotificationWork();

                    return;
                }

                FileSystemWatcher_RazorFileEvent(physicalFilePath, notification.ChangeKind.Value);
            }
        }

        private void FileSystemWatcher_RazorFileEvent(string physicalFilePath, RazorFileChangeKind kind)
        {
            foreach (var listener in _listeners)
            {
                listener.RazorFileChanged(physicalFilePath, kind);
            }
        }

        private void OnStartingDelayedNotificationWork()
        {
            if (BlockNotificationWorkStart != null)
            {
                BlockNotificationWorkStart.Wait();
                BlockNotificationWorkStart.Reset();
            }
        }

        private void OnNoopingNotificationWork()
        {
            if (NotifyNotificationNoop != null)
            {
                NotifyNotificationNoop.Set();
            }
        }

        // Internal for testing
        internal class DelayedFileChangeNotification
        {
            public Task NotifyTask { get; set; }

            public RazorFileChangeKind? ChangeKind { get; set; }
        }
    }
}
