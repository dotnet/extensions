// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class DefaultRazorProjectService : RazorProjectService
    {
        private readonly ProjectSnapshotManagerShimAccessor _projectSnapshotManagerAccessor;
        private readonly ForegroundDispatcherShim _foregroundDispatcher;
        private readonly ProjectResolver _projectResolver;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly VSCodeLogger _logger;

        public DefaultRazorProjectService(
            ForegroundDispatcherShim foregroundDispatcher,
            ProjectResolver projectResolver,
            FilePathNormalizer filePathNormalizer,
            ProjectSnapshotManagerShimAccessor projectSnapshotManagerAccessor,
            VSCodeLogger logger)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectResolver == null)
            {
                throw new ArgumentNullException(nameof(projectResolver));
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

            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
            _foregroundDispatcher = foregroundDispatcher;
            _projectResolver = projectResolver;
            _filePathNormalizer = filePathNormalizer;
            _logger = logger;
        }

        public override void AddDocument(SourceText sourceText, string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolveProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            var hostDocument = HostDocumentShim.Create(textDocumentPath, textDocumentPath);
            var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default);
            var textLoader = TextLoader.From(textAndVersion);
            _projectSnapshotManagerAccessor.Instance.DocumentAdded(projectSnapshot.HostProject, hostDocument, textLoader);

            _logger.Log($"Added document '{textDocumentPath}' to project '{projectSnapshot.FilePath}'.");
        }

        public override void RemoveDocument(string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolveProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }

            if (!projectSnapshot.DocumentFilePaths.Contains(textDocumentPath, FilePathComparerShim.Instance))
            {
                _logger.Log($"Containing project is not tracking document '{filePath}");
                return;
            }

            var document = projectSnapshot.GetDocument(textDocumentPath);
            _projectSnapshotManagerAccessor.Instance.DocumentRemoved(projectSnapshot.HostProject, document.HostDocument);

            _logger.Log($"Removed document '{textDocumentPath}' from project '{projectSnapshot.FilePath}'.");
        }

        public override void UpdateDocument(SourceText sourceText, string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var textDocumentPath = _filePathNormalizer.Normalize(filePath);
            if (!_projectResolver.TryResolveProject(textDocumentPath, out var projectSnapshot))
            {
                projectSnapshot = _projectResolver.GetMiscellaneousProject();
            }
            _projectSnapshotManagerAccessor.Instance.DocumentChanged(projectSnapshot.HostProject.FilePath, textDocumentPath, sourceText);

            _logger.Log($"Updated document '{textDocumentPath}'.");
        }

        public override void AddProject(string filePath, RazorConfiguration configuration)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            var hostProject = HostProjectShim.Create(normalizedPath, configuration);
            _projectSnapshotManagerAccessor.Instance.HostProjectAdded(hostProject);
            _logger.Log($"Added project '{filePath}' to project system.");

            TryMigrateMiscellaneousDocumentsToProject();
        }

        public override void RemoveProject(string filePath)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            var project = _projectSnapshotManagerAccessor.Instance.GetLoadedProject(normalizedPath);

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
        internal void TryMigrateDocumentsFromRemovedProject(ProjectSnapshotShim project)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var miscellaneousProject = _projectResolver.GetMiscellaneousProject();

            foreach (var documentFilePath in project.DocumentFilePaths)
            {
                var documentSnapshot = project.GetDocument(documentFilePath);

                if (!_projectResolver.TryResolveProject(documentFilePath, out var toProject))
                {
                    // This is the common case. It'd be rare for a project to be nested but we need to protect against it anyhow.
                    toProject = miscellaneousProject;
                }

                var textLoader = new DocumentSnapshotTextLoader(documentSnapshot);
                _projectSnapshotManagerAccessor.Instance.DocumentAdded(toProject.HostProject, documentSnapshot.HostDocument, textLoader);
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
                if (!_projectResolver.TryResolveProject(documentFilePath, out var projectSnapshot))
                {
                    continue;
                }

                var documentSnapshot = miscellaneousProject.GetDocument(documentFilePath);

                // Remove from miscellaneous project
                _projectSnapshotManagerAccessor.Instance.DocumentRemoved(miscellaneousProject.HostProject, documentSnapshot.HostDocument);

                // Add to new project

                var textLoader = new DocumentSnapshotTextLoader(documentSnapshot);
                _projectSnapshotManagerAccessor.Instance.DocumentAdded(projectSnapshot.HostProject, documentSnapshot.HostDocument, textLoader);

                _logger.Log($"Migrated '{documentFilePath}' from the '{miscellaneousProject.FilePath}' project to '{projectSnapshot.FilePath}' project.");
            }
        }
    }
}
