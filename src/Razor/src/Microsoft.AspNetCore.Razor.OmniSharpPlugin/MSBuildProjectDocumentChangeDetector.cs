// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using Microsoft.Build.Execution;
using OmniSharp.MSBuild.Notification;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    [Shared]
    [Export(typeof(IMSBuildEventSink))]
    internal class MSBuildProjectDocumentChangeDetector : IMSBuildEventSink
    {
        private const string MSBuildProjectFullPathPropertyName = "MSBuildProjectFullPath";
        private const string MSBuildProjectDirectoryPropertyName = "MSBuildProjectDirectory";
        private static readonly IReadOnlyList<string> RazorFileExtensions = new[] { "razor", "cshtml" };

        private readonly Dictionary<string, IReadOnlyList<FileSystemWatcher>> _watcherMap;
        private readonly IReadOnlyList<IRazorDocumentChangeListener> _documentChangeListeners;
        private readonly List<IRazorDocumentOutputChangeListener> _documentOutputChangeListeners;

        [ImportingConstructor]
        public MSBuildProjectDocumentChangeDetector(
            [ImportMany] IEnumerable<IRazorDocumentChangeListener> documentChangeListeners,
            [ImportMany] IEnumerable<IRazorDocumentOutputChangeListener> documentOutputChangeListeners)
        {
            if (documentChangeListeners == null)
            {
                throw new ArgumentNullException(nameof(documentChangeListeners));
            }

            if (documentOutputChangeListeners == null)
            {
                throw new ArgumentNullException(nameof(documentOutputChangeListeners));
            }

            _watcherMap = new Dictionary<string, IReadOnlyList<FileSystemWatcher>>(FilePathComparer.Instance);
            _documentChangeListeners = documentChangeListeners.ToList();
            _documentOutputChangeListeners = documentOutputChangeListeners.ToList();
        }

        public void ProjectLoaded(ProjectLoadedEventArgs loadedArgs)
        {
            if (loadedArgs == null)
            {
                throw new ArgumentNullException(nameof(loadedArgs));
            }

            var projectInstance = loadedArgs.ProjectInstance;
            var projectFilePath = projectInstance.GetPropertyValue(MSBuildProjectFullPathPropertyName);
            if (string.IsNullOrEmpty(projectFilePath))
            {
                // This should never be true but we're being extra careful.
                return;
            }

            var projectDirectory = projectInstance.GetPropertyValue(MSBuildProjectDirectoryPropertyName);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                // This should never be true but we're beign extra careful.
                return;
            }

            if (_watcherMap.TryGetValue(projectDirectory, out var existingWatchers))
            {
                for (var i = 0; i < existingWatchers.Count; i++)
                {
                    existingWatchers[i].Dispose();
                }
            }

            var watchers = new List<FileSystemWatcher>(RazorFileExtensions.Count);
            for (var i = 0; i < RazorFileExtensions.Count; i++)
            {
                var documentWatcher = new FileSystemWatcher(projectDirectory, "*." + RazorFileExtensions[i])
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = true,
                };

                documentWatcher.Created += (sender, args) => FileSystemWatcher_RazorDocumentEvent(args.FullPath, projectDirectory, projectInstance, RazorFileChangeKind.Added);
                documentWatcher.Deleted += (sender, args) => FileSystemWatcher_RazorDocumentEvent(args.FullPath, projectDirectory, projectInstance, RazorFileChangeKind.Removed);
                documentWatcher.Changed += (sender, args) => FileSystemWatcher_RazorDocumentEvent(args.FullPath, projectDirectory, projectInstance, RazorFileChangeKind.Changed);
                documentWatcher.Renamed += (sender, args) =>
                {
                    // Translate file renames into remove->add
                    FileSystemWatcher_RazorDocumentEvent(args.OldFullPath, projectDirectory, projectInstance, RazorFileChangeKind.Removed);
                    FileSystemWatcher_RazorDocumentEvent(args.FullPath, projectDirectory, projectInstance, RazorFileChangeKind.Added);
                };
                watchers.Add(documentWatcher);


                var documentOutputWatcher = new FileSystemWatcher(projectDirectory, "*." + RazorFileExtensions[i] + ".g.cs")
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = true,
                };

                documentOutputWatcher.Created += (sender, args) => FileSystemWatcher_RazorDocumentOutputEvent(args.FullPath, projectDirectory, projectInstance, RazorFileChangeKind.Added);
                documentOutputWatcher.Deleted += (sender, args) => FileSystemWatcher_RazorDocumentOutputEvent(args.FullPath, projectDirectory, projectInstance, RazorFileChangeKind.Removed);
                documentOutputWatcher.Changed += (sender, args) => FileSystemWatcher_RazorDocumentOutputEvent(args.FullPath, projectDirectory, projectInstance, RazorFileChangeKind.Changed);
                documentOutputWatcher.Renamed += (sender, args) =>
                {
                    // Translate file renames into remove->add
                    FileSystemWatcher_RazorDocumentOutputEvent(args.OldFullPath, projectDirectory, projectInstance, RazorFileChangeKind.Removed);
                    FileSystemWatcher_RazorDocumentOutputEvent(args.FullPath, projectDirectory, projectInstance, RazorFileChangeKind.Added);
                };
                watchers.Add(documentOutputWatcher);

                documentWatcher.EnableRaisingEvents = true;
                documentOutputWatcher.EnableRaisingEvents = true;
            }

            _watcherMap[projectDirectory] = watchers;
        }
        
        // Internal for testing
        internal void FileSystemWatcher_RazorDocumentEvent(string filePath, string projectDirectory, ProjectInstance projectInstance, RazorFileChangeKind changeKind)
        {
            var relativeFilePath = ResolveRelativeFilePath(filePath, projectDirectory);
            var args = new RazorFileChangeEventArgs(filePath, relativeFilePath, projectInstance, changeKind);
            for (var i = 0; i < _documentChangeListeners.Count; i++)
            {
                _documentChangeListeners[i].RazorDocumentChanged(args);
            }
        }

        // Internal for testing
        internal void FileSystemWatcher_RazorDocumentOutputEvent(string filePath, string projectDirectory, ProjectInstance projectInstance, RazorFileChangeKind changeKind)
        {
            var relativeFilePath = ResolveRelativeFilePath(filePath, projectDirectory);
            var args = new RazorFileChangeEventArgs(filePath, relativeFilePath, projectInstance, changeKind);
            for (var i = 0; i < _documentOutputChangeListeners.Count; i++)
            {
                _documentOutputChangeListeners[i].RazorDocumentOutputChanged(args);
            }
        }

        // Internal for testing
        internal static string ResolveRelativeFilePath(string filePath, string projectDirectory)
        {
            if (filePath.StartsWith(projectDirectory, FilePathComparison.Instance))
            {
                var relativePath = filePath.Substring(projectDirectory.Length + 1 /* Trailing slash */ );
                return relativePath;
            }

            return filePath;
        }
    }
}
