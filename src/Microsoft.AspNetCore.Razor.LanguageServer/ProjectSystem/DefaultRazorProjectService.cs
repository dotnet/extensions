// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class DefaultRazorProjectService : RazorProjectService
    {
        private readonly ProjectSnapshotManagerAccessor _projectSnapshotManagerAccessor;
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly HostDocumentFactory _hostDocumentFactory;
        private readonly ProjectResolver _projectResolver;
        private readonly DocumentVersionCache _documentVersionCache;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly DocumentResolver _documentResolver;
        private readonly VSCodeLogger _logger;

        public DefaultRazorProjectService(
            ForegroundDispatcher foregroundDispatcher,
            HostDocumentFactory hostDocumentFactory,
            DocumentResolver documentResolver,
            ProjectResolver projectResolver,
            DocumentVersionCache documentVersionCache,
            FilePathNormalizer filePathNormalizer,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor,
            VSCodeLogger logger)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (hostDocumentFactory == null)
            {
                throw new ArgumentNullException(nameof(hostDocumentFactory));
            }

            if (documentResolver == null)
            {
                throw new ArgumentNullException(nameof(documentResolver));
            }

            if (projectResolver == null)
            {
                throw new ArgumentNullException(nameof(projectResolver));
            }

            if (documentVersionCache == null)
            {
                throw new ArgumentNullException(nameof(documentVersionCache));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (projectSnapshotManagerAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _hostDocumentFactory = hostDocumentFactory;
            _documentResolver = documentResolver;
            _projectResolver = projectResolver;
            _documentVersionCache = documentVersionCache;
            _filePathNormalizer = filePathNormalizer;
            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
            _logger = logger;
        }

        public override void AddDocument(string filePath, TextLoader textLoader)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (_documentResolver.TryResolveDocument(textDocumentPath, out var _))
            {
                // Document already added. This usually occurs when VSCode has already pre-initialized
                // open documents and then we try to manually add all known razor documents.
                return;
            }

            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var hostDocument = _hostDocumentFactory.Create(textDocumentPath);
            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
            _projectSnapshotManagerAccessor.Instance.DocumentAdded(defaultProject.HostProject, hostDocument, textLoader);

            _logger.Log($"Added document '{textDocumentPath}' to project '{projectSnapshot.FilePath}'.");
        }

        public override void OpenDocument(string filePath, SourceText sourceText, long version)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_documentResolver.TryResolveDocument(textDocumentPath, out var _))
            {
                // Document hasn't been added. This usually occurs when VSCode trumps all other initialization 
                // processes and pre-initializes already open documents.
                AddDocument(filePath, textLoader: null);
            }

            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
            _projectSnapshotManagerAccessor.Instance.DocumentOpened(defaultProject.HostProject.FilePath, textDocumentPath, sourceText);

            TrackDocumentVersion(textDocumentPath, version);

            _logger.Log($"Opening document '{textDocumentPath}' in project '{projectSnapshot.FilePath}'.");
        }

        public override void CloseDocument(string filePath, TextLoader textLoader)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
            _projectSnapshotManagerAccessor.Instance.DocumentClosed(defaultProject.HostProject.FilePath, textDocumentPath, textLoader);

            _logger.Log($"Closing document '{textDocumentPath}' in project '{projectSnapshot.FilePath}'.");
        }

        public override void RemoveDocument(string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            if (!projectSnapshot.DocumentFilePaths.Contains(textDocumentPath, FilePathComparer.Instance))
            {
                _logger.Log($"Containing project is not tracking document '{filePath}");
                return;
            }

            var document = (DefaultDocumentSnapshot)projectSnapshot.GetDocument(textDocumentPath);
            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
            _projectSnapshotManagerAccessor.Instance.DocumentRemoved(defaultProject.HostProject, document.State.HostDocument);

            _logger.Log($"Removed document '{textDocumentPath}' from project '{projectSnapshot.FilePath}'.");
        }

        public override void UpdateDocument(string filePath, SourceText sourceText, long version)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolvePotentialProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
            _projectSnapshotManagerAccessor.Instance.DocumentChanged(defaultProject.HostProject.FilePath, textDocumentPath, sourceText);

            TrackDocumentVersion(textDocumentPath, version);

            _logger.Log($"Updated document '{textDocumentPath}'.");
        }

        public override void AddProject(string filePath, RazorConfiguration configuration)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            var hostProject = new HostProject(normalizedPath, configuration);
            _projectSnapshotManagerAccessor.Instance.HostProjectAdded(hostProject);
            _logger.Log($"Added project '{filePath}' to project system.");

            TryMigrateMiscellaneousDocumentsToProject();
        }

        public override void RemoveProject(string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            var project = (DefaultProjectSnapshot)_projectSnapshotManagerAccessor.Instance.GetLoadedProject(normalizedPath);

            if (project == null)
            {
                // Never tracked the project to begin with, noop.
                return;
            }

            _projectSnapshotManagerAccessor.Instance.HostProjectRemoved(project.HostProject);
            _logger.Log($"Removing project '{filePath}' from project system.");

            TryMigrateDocumentsFromRemovedProject(project);
        }

        // Internal for testing
        internal void TryMigrateDocumentsFromRemovedProject(ProjectSnapshot project)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var miscellaneousProject = _projectResolver.GetMiscellaneousProject();

            foreach (var documentFilePath in project.DocumentFilePaths)
            {
                var documentSnapshot = (DefaultDocumentSnapshot)project.GetDocument(documentFilePath);

                if (!_projectResolver.TryResolvePotentialProject(documentFilePath, out var toProject))
                {
                    // This is the common case. It'd be rare for a project to be nested but we need to protect against it anyhow.
                    toProject = miscellaneousProject;
                }

                var textLoader = new DocumentSnapshotTextLoader(documentSnapshot);
                var defaultToProject = (DefaultProjectSnapshot)toProject;
                _projectSnapshotManagerAccessor.Instance.DocumentAdded(defaultToProject.HostProject, documentSnapshot.State.HostDocument, textLoader);
                _logger.Log($"Migrated '{documentFilePath}' from the '{project.FilePath}' project to '{toProject.FilePath}' project.");
            }
        }

        // Internal for testing
        internal void TryMigrateMiscellaneousDocumentsToProject()
        {
            _foregroundDispatcher.AssertForegroundThread();

            var miscellaneousProject = _projectResolver.GetMiscellaneousProject();

            foreach (var documentFilePath in miscellaneousProject.DocumentFilePaths)
            {
                if (!_projectResolver.TryResolvePotentialProject(documentFilePath, out var projectSnapshot))
                {
                    continue;
                }

                var documentSnapshot = (DefaultDocumentSnapshot)miscellaneousProject.GetDocument(documentFilePath);

                // Remove from miscellaneous project
                var defaultMiscProject = (DefaultProjectSnapshot)miscellaneousProject;
                _projectSnapshotManagerAccessor.Instance.DocumentRemoved(defaultMiscProject.HostProject, documentSnapshot.State.HostDocument);

                // Add to new project

                var textLoader = new DocumentSnapshotTextLoader(documentSnapshot);
                var defaultProject = (DefaultProjectSnapshot)projectSnapshot;
                _projectSnapshotManagerAccessor.Instance.DocumentAdded(defaultProject.HostProject, documentSnapshot.State.HostDocument, textLoader);

                _logger.Log($"Migrated '{documentFilePath}' from the '{miscellaneousProject.FilePath}' project to '{projectSnapshot.FilePath}' project.");
            }
        }

        private void TrackDocumentVersion(string textDocumentPath, long version)
        {
            if (!_documentResolver.TryResolveDocument(textDocumentPath, out var documentSnapshot))
            {
                return;
            }

            _documentVersionCache.TrackDocumentVersion(documentSnapshot, version);
        }
    }
}
